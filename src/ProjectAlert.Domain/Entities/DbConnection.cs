using Dapper.Contrib.Extensions;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 数据库连接配置
/// </summary>
[Table("db_connections")]
public class DbConnection : BaseEntity
{
    /// <summary>
    /// 连接名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DbType DbType { get; set; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}
