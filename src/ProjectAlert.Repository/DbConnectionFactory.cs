using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using ProjectAlert.Domain.Entities;
using DbType = ProjectAlert.Domain.Enums.DbType;

namespace ProjectAlert.Repository;

/// <summary>
/// 数据库连接工厂接口
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// 根据配置创建数据库连接
    /// </summary>
    /// <param name="config">数据库连接配置</param>
    /// <returns>数据库连接</returns>
    IDbConnection CreateConnection(DbConnection config);
}

/// <summary>
/// 数据库连接工厂
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    /// <summary>
    /// 根据配置创建数据库连接
    /// </summary>
    public IDbConnection CreateConnection(DbConnection config)
    {
        return config.DbType switch
        {
            DbType.MySql => new MySqlConnection(config.ConnectionString),
            DbType.SqlServer => new SqlConnection(config.ConnectionString),
            DbType.Sqlite => new SqliteConnection(config.ConnectionString),
            _ => throw new NotSupportedException($"不支持的数据库类型: {config.DbType}")
        };
    }
}
