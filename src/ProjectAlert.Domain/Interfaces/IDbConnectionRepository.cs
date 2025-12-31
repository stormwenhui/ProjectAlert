using ProjectAlert.Domain.Entities;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 数据库连接仓储接口
/// </summary>
public interface IDbConnectionRepository : IRepository<DbConnection>
{
    /// <summary>
    /// 获取所有启用的数据库连接
    /// </summary>
    /// <returns>启用的连接集合</returns>
    Task<IEnumerable<DbConnection>> GetEnabledAsync();

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <param name="connection">连接配置</param>
    /// <returns>是否连接成功</returns>
    Task<bool> TestConnectionAsync(DbConnection connection);
}
