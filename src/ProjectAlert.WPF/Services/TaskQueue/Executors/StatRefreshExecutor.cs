using System.Net.Http;
using System.Text.Json;
using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.Repository;

namespace ProjectAlert.WPF.Services.TaskQueue.Executors;

/// <summary>
/// 缓存条目（包含数据和时间戳）
/// </summary>
internal class CacheEntry
{
    public List<Dictionary<string, object>> Data { get; set; } = [];
    public DateTime LastAccess { get; set; } = DateTime.Now;
}

/// <summary>
/// 统计数据刷新执行器
/// </summary>
public class StatRefreshExecutor : ITaskExecutor
{
    private readonly IStatConfigRepository _statConfigRepository;
    private readonly IDbConnectionRepository _dbConnectionRepository;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LogService _logService;

    // 用于计算变化量的历史数据缓存（带过期机制）
    private static readonly Dictionary<int, CacheEntry> _previousDataCache = new();
    private static readonly object _cacheLock = new();

    // 缓存配置
    private const int MaxCacheEntries = 20;      // 最多缓存20个配置的数据
    private const int MaxRowsPerEntry = 500;     // 每个配置最多缓存500行
    private const int CacheExpiryMinutes = 30;   // 缓存30分钟过期

    public TaskType SupportedType => TaskType.StatRefresh;

    public StatRefreshExecutor(
        IStatConfigRepository statConfigRepository,
        IDbConnectionRepository dbConnectionRepository,
        IDbConnectionFactory dbConnectionFactory,
        IHttpClientFactory httpClientFactory,
        LogService logService)
    {
        _statConfigRepository = statConfigRepository;
        _dbConnectionRepository = dbConnectionRepository;
        _dbConnectionFactory = dbConnectionFactory;
        _httpClientFactory = httpClientFactory;
        _logService = logService;
    }

    public async Task<object?> ExecuteAsync(TaskRequest request)
    {
        if (!request.TargetId.HasValue)
            throw new ArgumentException("StatRefresh 任务需要 TargetId（StatConfigId）");

        var configId = request.TargetId.Value;
        _logService.TaskQueueLog("Executor开始", request.TaskKey, $"StatRefreshExecutor.ExecuteAsync 开始, ConfigId={configId}");

        var config = await _statConfigRepository.GetByIdAsync(configId);

        if (config == null)
            throw new InvalidOperationException($"未找到统计配置: {configId}");

        _logService.TaskQueueLog("Executor配置", request.TaskKey, $"配置名称={config.Name}, 类型={config.SourceType}");

        // 获取数据库连接
        DbConnection? dbConnection = null;
        if (config.DbConnectionId.HasValue)
        {
            dbConnection = await _dbConnectionRepository.GetByIdAsync(config.DbConnectionId.Value);
        }

        // 根据数据源类型执行查询
        _logService.TaskQueueLog("Executor查询", request.TaskKey, $"开始执行 {config.SourceType} 查询");
        var queryStart = DateTime.Now;

        var (columns, rows) = config.SourceType switch
        {
            SourceType.Sql => await ExecuteSqlAsync(config, dbConnection),
            SourceType.Api => await ExecuteApiAsync(config),
            _ => throw new NotSupportedException($"不支持的数据源类型: {config.SourceType}")
        };

        var queryDuration = DateTime.Now - queryStart;
        _logService.TaskQueueLog("Executor查询完成", request.TaskKey,
            $"查询耗时={queryDuration.TotalMilliseconds:F0}ms, 行数={rows.Count}, 列数={columns.Count}");

        // 计算显示数据（含变化标记）
        var displayRows = ApplyChangeMarkers(configId, rows);

        var result = new StatRefreshResult
        {
            StatConfigId = configId,
            Columns = columns,
            Rows = rows,
            DisplayRows = displayRows,
            UpdateTime = DateTime.Now
        };

        _logService.TaskQueueLog("Executor完成", request.TaskKey, $"返回数据行数={result.Rows.Count}");

        return result;
    }

    private async Task<(List<string> Columns, List<Dictionary<string, object>> Rows)> ExecuteSqlAsync(
        StatConfig config, DbConnection? dbConnection)
    {
        if (dbConnection == null)
            throw new InvalidOperationException("未配置数据库连接");

        if (string.IsNullOrEmpty(config.SqlQuery))
            throw new InvalidOperationException("未配置SQL查询语句");

        using var connection = _dbConnectionFactory.CreateConnection(dbConnection);
        if (connection is System.Data.Common.DbConnection conn)
        {
            await conn.OpenAsync();
        }
        else
        {
            connection.Open();
        }

        var results = await connection.QueryAsync<dynamic>(config.SqlQuery);
        var dataList = results.ToList();

        var columns = new List<string>();
        var rows = new List<Dictionary<string, object>>();

        if (dataList.Count > 0)
        {
            var firstRow = (IDictionary<string, object>)dataList[0];
            columns.AddRange(firstRow.Keys);

            foreach (var row in dataList)
            {
                var dict = (IDictionary<string, object>)row;
                rows.Add(new Dictionary<string, object>(dict));
            }
        }

        return (columns, rows);
    }

    private async Task<(List<string> Columns, List<Dictionary<string, object>> Rows)> ExecuteApiAsync(StatConfig config)
    {
        if (string.IsNullOrEmpty(config.ApiUrl))
            throw new InvalidOperationException("未配置API地址");

        var httpClient = _httpClientFactory.CreateClient();

        // 使用 CancellationToken 控制超时，而不是修改 HttpClient.Timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.ApiTimeout));

        // 创建请求消息，在请求级别设置 Headers（避免污染 HttpClient）
        using var request = new HttpRequestMessage(
            config.ApiMethod?.ToUpper() == "POST" ? HttpMethod.Post : HttpMethod.Get,
            config.ApiUrl);

        // 设置请求头到请求消息（而非 HttpClient.DefaultRequestHeaders）
        if (!string.IsNullOrEmpty(config.ApiHeaders))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(config.ApiHeaders);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }

        // 设置 POST 请求体
        if (config.ApiMethod?.ToUpper() == "POST")
        {
            request.Content = new StringContent(config.ApiBody ?? string.Empty, System.Text.Encoding.UTF8, "application/json");
        }

        var response = await httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        // 解析JSON数据
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // 如果指定了数据路径，按路径获取数据
        if (!string.IsNullOrEmpty(config.DataPath))
        {
            var paths = config.DataPath.Split('.');
            foreach (var path in paths)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(path, out var next))
                {
                    jsonElement = next;
                }
                else
                {
                    throw new InvalidOperationException($"无法在JSON中找到路径: {config.DataPath}");
                }
            }
        }

        var columns = new List<string>();
        var rows = new List<Dictionary<string, object>>();

        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            var array = jsonElement.EnumerateArray().ToList();
            if (array.Count > 0 && array[0].ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in array[0].EnumerateObject())
                {
                    columns.Add(prop.Name);
                }

                foreach (var item in array)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in item.EnumerateObject())
                    {
                        dict[prop.Name] = GetJsonValue(prop.Value);
                    }
                    rows.Add(dict);
                }
            }
        }

        return (columns, rows);
    }

    private static object GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            _ => element.ToString()
        };
    }

    private List<Dictionary<string, object>> ApplyChangeMarkers(int configId, List<Dictionary<string, object>> newData)
    {
        List<Dictionary<string, object>>? previousData = null;

        lock (_cacheLock)
        {
            // 清理过期缓存
            CleanExpiredCache();

            if (_previousDataCache.TryGetValue(configId, out var cacheEntry))
            {
                previousData = cacheEntry.Data;
                cacheEntry.LastAccess = DateTime.Now;
            }
        }

        if (previousData == null || previousData.Count == 0 || newData.Count == 0)
        {
            // 首次加载或无数据，保存当前数据并直接返回
            lock (_cacheLock)
            {
                // 限制缓存行数
                var dataToCache = newData.Count > MaxRowsPerEntry
                    ? newData.Take(MaxRowsPerEntry).Select(d => new Dictionary<string, object>(d)).ToList()
                    : newData.Select(d => new Dictionary<string, object>(d)).ToList();

                _previousDataCache[configId] = new CacheEntry { Data = dataToCache, LastAccess = DateTime.Now };
            }
            return newData;
        }

        var displayRows = new List<Dictionary<string, object>>();

        // 比较并添加变化标记
        for (int i = 0; i < newData.Count; i++)
        {
            var newRow = newData[i];
            var displayRow = new Dictionary<string, object>();

            // 尝试找到对应的旧行（按行索引匹配）
            Dictionary<string, object>? oldRow = i < previousData.Count ? previousData[i] : null;

            foreach (var kvp in newRow)
            {
                var key = kvp.Key;
                var newValue = kvp.Value;

                // 检查是否为数值类型
                if (oldRow != null && oldRow.TryGetValue(key, out var oldValue) && IsNumeric(newValue) && IsNumeric(oldValue))
                {
                    var newNum = Convert.ToDecimal(newValue);
                    var oldNum = Convert.ToDecimal(oldValue);
                    var diff = newNum - oldNum;

                    if (diff != 0)
                    {
                        // 有变化，添加标记
                        var sign = diff > 0 ? "+" : "";
                        displayRow[key] = $"{newValue}（{sign}{diff}）";
                    }
                    else
                    {
                        displayRow[key] = newValue;
                    }
                }
                else
                {
                    displayRow[key] = newValue;
                }
            }

            displayRows.Add(displayRow);
        }

        // 保存当前数据作为下次比较的基准
        lock (_cacheLock)
        {
            var dataToCache = newData.Count > MaxRowsPerEntry
                ? newData.Take(MaxRowsPerEntry).Select(d => new Dictionary<string, object>(d)).ToList()
                : newData.Select(d => new Dictionary<string, object>(d)).ToList();

            _previousDataCache[configId] = new CacheEntry { Data = dataToCache, LastAccess = DateTime.Now };
        }

        return displayRows;
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    private static void CleanExpiredCache()
    {
        var now = DateTime.Now;
        var expiredKeys = _previousDataCache
            .Where(kvp => (now - kvp.Value.LastAccess).TotalMinutes > CacheExpiryMinutes)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _previousDataCache.Remove(key);
        }

        // 如果缓存仍然超过限制，移除最旧的条目
        while (_previousDataCache.Count > MaxCacheEntries)
        {
            var oldestKey = _previousDataCache
                .OrderBy(kvp => kvp.Value.LastAccess)
                .First().Key;
            _previousDataCache.Remove(oldestKey);
        }
    }

    private static bool IsNumeric(object? value)
    {
        if (value == null) return false;
        return value is sbyte or byte or short or ushort or int or uint or long or ulong
            or float or double or decimal;
    }
}
