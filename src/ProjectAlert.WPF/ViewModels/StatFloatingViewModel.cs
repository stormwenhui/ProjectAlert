using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Repository;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 统计浮窗视图模型
/// </summary>
public partial class StatFloatingViewModel : FloatingWidgetViewModelBase
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    /// <summary>
    /// 统计配置
    /// </summary>
    [ObservableProperty]
    private StatConfig _statConfig = null!;

    /// <summary>
    /// 统计配置ID
    /// </summary>
    public int StatConfigId => StatConfig?.Id ?? 0;

    /// <summary>
    /// 查询结果数据
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Dictionary<string, object>> _data = [];

    /// <summary>
    /// 上一次的数据快照（用于计算变化量）
    /// </summary>
    private List<Dictionary<string, object>> _previousData = [];

    /// <summary>
    /// 列名列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _columns = [];

    /// <summary>
    /// 数据库连接
    /// </summary>
    public DbConnection? DbConnection { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public StatFloatingViewModel(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    /// <summary>
    /// 初始化统计配置
    /// </summary>
    public void Initialize(StatConfig config, DbConnection? dbConnection)
    {
        StatConfig = config;
        DbConnection = dbConnection;
        Title = config.Name;
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    public override async Task RefreshAsync()
    {
        if (StatConfig == null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            switch (StatConfig.SourceType)
            {
                case SourceType.Sql:
                    await RefreshFromSqlAsync();
                    break;
                case SourceType.Api:
                    await RefreshFromApiAsync();
                    break;
            }
            LastUpdateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 从SQL刷新数据
    /// </summary>
    private async Task RefreshFromSqlAsync()
    {
        if (DbConnection == null)
            throw new InvalidOperationException("未配置数据库连接");

        if (string.IsNullOrEmpty(StatConfig.SqlQuery))
            throw new InvalidOperationException("未配置SQL查询语句");

        using var connection = _dbConnectionFactory.CreateConnection(DbConnection);
        if (connection is System.Data.Common.DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();
        }
        else
        {
            connection.Open();
        }

        var results = await connection.QueryAsync<dynamic>(StatConfig.SqlQuery);
        var dataList = results.ToList();

        Data.Clear();
        Columns.Clear();

        if (dataList.Count > 0)
        {
            // 获取列名
            var firstRow = (IDictionary<string, object>)dataList[0];
            foreach (var key in firstRow.Keys)
            {
                Columns.Add(key);
            }

            // 收集新数据
            var newData = new List<Dictionary<string, object>>();
            foreach (var row in dataList)
            {
                var dict = (IDictionary<string, object>)row;
                newData.Add(new Dictionary<string, object>(dict));
            }

            // 应用变化标记并填充数据
            ApplyChangeMarkers(newData);
        }
    }

    /// <summary>
    /// 从API刷新数据
    /// </summary>
    private async Task RefreshFromApiAsync()
    {
        if (string.IsNullOrEmpty(StatConfig.ApiUrl))
            throw new InvalidOperationException("未配置API地址");

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(StatConfig.ApiTimeout);

        // 设置请求头
        if (!string.IsNullOrEmpty(StatConfig.ApiHeaders))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(StatConfig.ApiHeaders);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }

        HttpResponseMessage response;
        if (StatConfig.ApiMethod?.ToUpper() == "POST")
        {
            var content = new StringContent(StatConfig.ApiBody ?? string.Empty, System.Text.Encoding.UTF8, "application/json");
            response = await httpClient.PostAsync(StatConfig.ApiUrl, content);
        }
        else
        {
            response = await httpClient.GetAsync(StatConfig.ApiUrl);
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        // 解析JSON数据
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // 如果指定了数据路径，按路径获取数据
        if (!string.IsNullOrEmpty(StatConfig.DataPath))
        {
            var paths = StatConfig.DataPath.Split('.');
            foreach (var path in paths)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(path, out var next))
                {
                    jsonElement = next;
                }
                else
                {
                    throw new InvalidOperationException($"无法在JSON中找到路径: {StatConfig.DataPath}");
                }
            }
        }

        Data.Clear();
        Columns.Clear();

        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            var array = jsonElement.EnumerateArray().ToList();
            if (array.Count > 0)
            {
                // 获取列名
                if (array[0].ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in array[0].EnumerateObject())
                    {
                        Columns.Add(prop.Name);
                    }

                    // 收集新数据
                    var newData = new List<Dictionary<string, object>>();
                    foreach (var item in array)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            dict[prop.Name] = GetJsonValue(prop.Value);
                        }
                        newData.Add(dict);
                    }

                    // 应用变化标记并填充数据
                    ApplyChangeMarkers(newData);
                }
            }
        }
    }

    /// <summary>
    /// 获取JSON值
    /// </summary>
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

    /// <summary>
    /// 应用变化标记到数据
    /// </summary>
    private void ApplyChangeMarkers(List<Dictionary<string, object>> newData)
    {
        if (_previousData.Count == 0 || newData.Count == 0)
        {
            // 首次加载或无数据，保存当前数据并直接显示
            _previousData = newData.Select(d => new Dictionary<string, object>(d)).ToList();
            foreach (var row in newData)
            {
                Data.Add(row);
            }
            return;
        }

        // 比较并添加变化标记
        for (int i = 0; i < newData.Count; i++)
        {
            var newRow = newData[i];
            var displayRow = new Dictionary<string, object>();

            // 尝试找到对应的旧行（按行索引匹配）
            Dictionary<string, object>? oldRow = i < _previousData.Count ? _previousData[i] : null;

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

            Data.Add(displayRow);
        }

        // 保存当前数据作为下次比较的基准
        _previousData = newData.Select(d => new Dictionary<string, object>(d)).ToList();
    }

    /// <summary>
    /// 判断是否为数值类型
    /// </summary>
    private static bool IsNumeric(object? value)
    {
        if (value == null) return false;
        return value is sbyte or byte or short or ushort or int or uint or long or ulong
            or float or double or decimal;
    }
}
