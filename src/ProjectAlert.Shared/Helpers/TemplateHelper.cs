using System.Text.RegularExpressions;

namespace ProjectAlert.Shared.Helpers;

/// <summary>
/// 模板帮助类（用于渲染预警消息模板）
/// </summary>
public static partial class TemplateHelper
{
    // 匹配 {$xxx} 或 {$table.xxx} 或 {$api.xxx} 格式的变量
    [GeneratedRegex(@"\{\$([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)\}")]
    private static partial Regex VariablePattern();

    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="template">模板字符串</param>
    /// <param name="variables">变量字典</param>
    /// <returns>渲染后的字符串</returns>
    public static string Render(string? template, IDictionary<string, object?> variables)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        return VariablePattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return GetValue(variables, key)?.ToString() ?? match.Value;
        });
    }

    /// <summary>
    /// 从变量字典中获取值（支持嵌套路径）
    /// </summary>
    private static object? GetValue(IDictionary<string, object?> variables, string path)
    {
        var segments = path.Split('.');
        object? current = variables;

        foreach (var segment in segments)
        {
            if (current == null) return null;

            if (current is IDictionary<string, object?> dict)
            {
                if (dict.TryGetValue(segment, out var value))
                {
                    current = value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // 尝试反射获取属性
                var property = current.GetType().GetProperty(segment);
                if (property != null)
                {
                    current = property.GetValue(current);
                }
                else
                {
                    return null;
                }
            }
        }

        return current;
    }

    /// <summary>
    /// 创建系统变量字典
    /// </summary>
    public static Dictionary<string, object?> CreateSystemVariables(
        string? name = null,
        string? system = null,
        string? level = null,
        string? threshold = null,
        int? rowCount = null)
    {
        return new Dictionary<string, object?>
        {
            ["name"] = name,
            ["system"] = system,
            ["level"] = level,
            ["threshold"] = threshold,
            ["rowCount"] = rowCount
        };
    }
}
