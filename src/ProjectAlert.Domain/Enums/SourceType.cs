using System.ComponentModel;

namespace ProjectAlert.Domain.Enums;

/// <summary>
/// 数据源类型
/// </summary>
public enum SourceType
{
    /// <summary>
    /// SQL 数据库
    /// </summary>
    [Description("SQL")]
    Sql,

    /// <summary>
    /// API 接口
    /// </summary>
    [Description("API")]
    Api
}
