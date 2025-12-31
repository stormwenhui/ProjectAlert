using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProjectAlert.Shared.Helpers;

/// <summary>
/// JSON 帮助类
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 序列化对象为 JSON 字符串
    /// </summary>
    public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(obj, options ?? DefaultOptions);

    /// <summary>
    /// 反序列化 JSON 字符串为对象
    /// </summary>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    /// <summary>
    /// 尝试反序列化
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result, JsonSerializerOptions? options = null)
    {
        try
        {
            result = Deserialize<T>(json, options);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// 根据路径获取 JSON 值
    /// </summary>
    public static JsonNode? GetValueByPath(string json, string path)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var node = JsonNode.Parse(json);
            if (node == null) return null;

            var segments = path.Split('.');
            foreach (var segment in segments)
            {
                if (node == null) return null;

                // 处理数组索引 [0]
                if (segment.Contains('[') && segment.Contains(']'))
                {
                    var bracketStart = segment.IndexOf('[');
                    var bracketEnd = segment.IndexOf(']');
                    var propertyName = segment[..bracketStart];
                    var indexStr = segment[(bracketStart + 1)..bracketEnd];

                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        node = node[propertyName];
                    }

                    if (int.TryParse(indexStr, out var index) && node is JsonArray array)
                    {
                        node = array[index];
                    }
                }
                else
                {
                    node = node[segment];
                }
            }

            return node;
        }
        catch
        {
            return null;
        }
    }
}
