using System.ComponentModel;

namespace ProjectAlert.Domain.Enums;

/// <summary>
/// 数据库类型
/// </summary>
public enum DbType
{
    /// <summary>
    /// MySQL
    /// </summary>
    [Description("MySQL")]
    MySql,

    /// <summary>
    /// SQL Server
    /// </summary>
    [Description("SQL Server")]
    SqlServer,

    /// <summary>
    /// SQLite
    /// </summary>
    [Description("SQLite")]
    Sqlite
}
