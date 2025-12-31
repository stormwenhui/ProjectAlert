using System.ComponentModel;

namespace ProjectAlert.Domain.Enums;

/// <summary>
/// API 判断层级
/// </summary>
public enum JudgeLevel
{
    /// <summary>
    /// 请求层 - 仅判断请求成功/失败、状态码
    /// </summary>
    [Description("请求层")]
    Request,

    /// <summary>
    /// 响应文本层 - 响应体字符串匹配
    /// </summary>
    [Description("响应文本层")]
    ResponseText,

    /// <summary>
    /// JSON解析层 - 解析JSON，按路径提取数据后判断
    /// </summary>
    [Description("JSON解析层")]
    JsonParse
}
