namespace ProjectAlert.Domain.Enums;

/// <summary>
/// 判断方式
/// </summary>
public enum JudgeType
{
    // ===== SQL 数据源 =====

    /// <summary>
    /// SQL 单值预警
    /// </summary>
    单值预警 = 1,

    /// <summary>
    /// SQL 多行预警
    /// </summary>
    多行预警 = 2,

    // ===== API 数据源 =====

    /// <summary>
    /// API 请求层判断（状态码/请求失败）
    /// </summary>
    请求层判断 = 3,

    /// <summary>
    /// API 响应文本层判断（包含/不包含）
    /// </summary>
    响应文本层判断 = 4,

    /// <summary>
    /// API JSON解析层 - 单值
    /// </summary>
    JSON解析层_单值 = 5,

    /// <summary>
    /// API JSON解析层 - 多行
    /// </summary>
    JSON解析层_多行 = 6
}
