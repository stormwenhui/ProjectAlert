using System.Net.Http;
using System.Text;
using System.Text.Json;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.WPF.Services.Executor;

/// <summary>
/// API 预警执行器
/// </summary>
public class ApiAlertExecutor : IAlertExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClientFactory">HTTP 客户端工厂</param>
    public ApiAlertExecutor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 执行 API 预警检查
    /// </summary>
    /// <param name="rule">预警规则</param>
    /// <returns>执行结果列表</returns>
    public async Task<List<ExecuteResult>> ExecuteAsync(AlertRule rule)
    {
        var results = new List<ExecuteResult>();

        if (string.IsNullOrEmpty(rule.ApiUrl))
            throw new InvalidOperationException("未配置 API URL");

        // 创建 HTTP 客户端
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(rule.ApiTimeout);

        // 解析请求头
        if (!string.IsNullOrEmpty(rule.ApiHeaders))
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(rule.ApiHeaders);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        // 发送请求
        HttpResponseMessage response;
        var method = rule.ApiMethod?.ToUpperInvariant() ?? "GET";

        switch (method)
        {
            case "GET":
                response = await client.GetAsync(rule.ApiUrl);
                break;
            case "POST":
                var content = new StringContent(rule.ApiBody ?? string.Empty, Encoding.UTF8, "application/json");
                response = await client.PostAsync(rule.ApiUrl, content);
                break;
            default:
                throw new NotSupportedException($"不支持的 HTTP 方法: {method}");
        }

        // 根据判断层级处理结果
        switch (rule.JudgeLevel)
        {
            case JudgeLevel.Request:
                // 请求层判断 - 检查响应状态码
                if (!response.IsSuccessStatusCode)
                {
                    results.Add(new ExecuteResult
                    {
                        AlertKey = rule.ApiUrl,
                        Message = FormatMessage(rule.MessageTemplate, new Dictionary<string, object>
                        {
                            ["StatusCode"] = (int)response.StatusCode,
                            ["ReasonPhrase"] = response.ReasonPhrase ?? string.Empty
                        })
                    });
                }
                break;

            case JudgeLevel.ResponseText:
                // 响应文本层判断 - 检查响应内容
                var text = await response.Content.ReadAsStringAsync();
                if (ShouldAlertByText(text, rule))
                {
                    results.Add(new ExecuteResult
                    {
                        AlertKey = rule.ApiUrl,
                        Message = FormatMessage(rule.MessageTemplate, new Dictionary<string, object>
                        {
                            ["ResponseText"] = text.Length > 200 ? text[..200] + "..." : text
                        }),
                        RawData = text
                    });
                }
                break;

            case JudgeLevel.JsonParse:
                // JSON解析层判断 - 解析 JSON 并检查指定字段
                var json = await response.Content.ReadAsStringAsync();
                var jsonResults = ParseJsonAndCheck(json, rule);
                results.AddRange(jsonResults);
                break;
        }

        return results;
    }

    /// <summary>
    /// 根据文本内容判断是否预警
    /// </summary>
    /// <param name="text">响应文本</param>
    /// <param name="rule">预警规则</param>
    /// <returns>是否预警</returns>
    private bool ShouldAlertByText(string text, AlertRule rule)
    {
        if (string.IsNullOrEmpty(rule.JudgeValue))
            return false;

        return rule.JudgeOperator switch
        {
            JudgeOperator.包含 => text.Contains(rule.JudgeValue),
            JudgeOperator.不包含 => !text.Contains(rule.JudgeValue),
            _ => false
        };
    }

    /// <summary>
    /// 解析 JSON 并检查预警条件
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <param name="rule">预警规则</param>
    /// <returns>执行结果列表</returns>
    private List<ExecuteResult> ParseJsonAndCheck(string json, AlertRule rule)
    {
        var results = new List<ExecuteResult>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 获取数据路径下的数据
            var data = GetJsonValue(root, rule.DataPath);

            if (data.ValueKind == JsonValueKind.Array)
            {
                // 数组类型 - 遍历检查每个元素
                foreach (var item in data.EnumerateArray())
                {
                    if (ShouldAlertByJson(item, rule))
                    {
                        var alertKey = GetJsonAlertKey(item, rule.KeyField);
                        var dict = JsonElementToDict(item);
                        results.Add(new ExecuteResult
                        {
                            AlertKey = alertKey,
                            Message = FormatMessage(rule.MessageTemplate, dict),
                            RawData = item.GetRawText()
                        });
                    }
                }
            }
            else if (data.ValueKind == JsonValueKind.Object)
            {
                // 对象类型 - 直接检查
                if (ShouldAlertByJson(data, rule))
                {
                    var alertKey = GetJsonAlertKey(data, rule.KeyField);
                    var dict = JsonElementToDict(data);
                    results.Add(new ExecuteResult
                    {
                        AlertKey = alertKey,
                        Message = FormatMessage(rule.MessageTemplate, dict),
                        RawData = data.GetRawText()
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            results.Add(new ExecuteResult
            {
                AlertKey = rule.ApiUrl,
                Message = $"JSON 解析失败: {ex.Message}",
                RawData = json
            });
        }

        return results;
    }

    /// <summary>
    /// 根据路径获取 JSON 值
    /// </summary>
    /// <param name="root">根元素</param>
    /// <param name="path">路径（用.分隔）</param>
    /// <returns>JSON 元素</returns>
    private JsonElement GetJsonValue(JsonElement root, string? path)
    {
        if (string.IsNullOrEmpty(path))
            return root;

        var current = root;
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var prop))
            {
                current = prop;
            }
            else
            {
                return default;
            }
        }

        return current;
    }

    /// <summary>
    /// 根据 JSON 元素判断是否预警
    /// </summary>
    /// <param name="element">JSON 元素</param>
    /// <param name="rule">预警规则</param>
    /// <returns>是否预警</returns>
    private bool ShouldAlertByJson(JsonElement element, AlertRule rule)
    {
        // JSON 多行预警 - 每行数据都触发预警
        if (rule.JudgeType == JudgeType.JSON解析层_多行)
            return true;

        if (string.IsNullOrEmpty(rule.JudgeField))
            return false;

        if (!element.TryGetProperty(rule.JudgeField, out var fieldValue))
            return false;

        var actualValue = GetJsonValueAsDouble(fieldValue);
        var expectedValue = double.TryParse(rule.JudgeValue, out var v) ? v : 0;

        return rule.JudgeOperator switch
        {
            JudgeOperator.等于 => actualValue == expectedValue,
            JudgeOperator.不等于 => actualValue != expectedValue,
            JudgeOperator.大于 => actualValue > expectedValue,
            JudgeOperator.大于等于 => actualValue >= expectedValue,
            JudgeOperator.小于 => actualValue < expectedValue,
            JudgeOperator.小于等于 => actualValue <= expectedValue,
            _ => false
        };
    }

    /// <summary>
    /// 获取 JSON 预警唯一标识
    /// </summary>
    /// <param name="element">JSON 元素</param>
    /// <param name="keyField">主键字段</param>
    /// <returns>唯一标识</returns>
    private string? GetJsonAlertKey(JsonElement element, string? keyField)
    {
        if (string.IsNullOrEmpty(keyField))
            return null;

        if (element.TryGetProperty(keyField, out var value))
            return value.ToString();

        return null;
    }

    /// <summary>
    /// 将 JSON 值转换为 double
    /// </summary>
    /// <param name="element">JSON 元素</param>
    /// <returns>数值</returns>
    private double GetJsonValueAsDouble(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => double.TryParse(element.GetString(), out var v) ? v : 0,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => 0
        };
    }

    /// <summary>
    /// 将 JSON 元素转换为字典
    /// </summary>
    /// <param name="element">JSON 元素</param>
    /// <returns>字典</returns>
    private Dictionary<string, object> JsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString();
            }
        }
        return dict;
    }

    /// <summary>
    /// 格式化预警消息
    /// </summary>
    /// <param name="template">消息模板</param>
    /// <param name="data">数据字典</param>
    /// <returns>格式化后的消息</returns>
    private string FormatMessage(string? template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template))
            return JsonSerializer.Serialize(data);

        var message = template;
        foreach (var kvp in data)
        {
            // 只支持 {api.参数名} 格式
            message = message.Replace($"{{api.{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }
        return message;
    }
}
