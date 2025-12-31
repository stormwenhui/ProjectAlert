using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 预警规则仓储接口
/// </summary>
public interface IAlertRuleRepository : IRepository<AlertRule>
{
    /// <summary>
    /// 获取所有启用的预警规则
    /// </summary>
    /// <returns>启用的预警规则集合</returns>
    Task<IEnumerable<AlertRule>> GetEnabledAsync();

    /// <summary>
    /// 根据系统分类获取预警规则
    /// </summary>
    /// <param name="category">系统分类</param>
    /// <returns>预警规则集合</returns>
    Task<IEnumerable<AlertRule>> GetByCategoryAsync(SystemCategory category);

    /// <summary>
    /// 获取预警规则并关联数据库连接信息
    /// </summary>
    /// <param name="id">规则ID</param>
    /// <returns>包含数据库连接的预警规则</returns>
    Task<AlertRule?> GetWithDbConnectionAsync(int id);

    /// <summary>
    /// 更新规则运行状态
    /// </summary>
    /// <param name="id">规则ID</param>
    /// <param name="success">是否执行成功</param>
    /// <param name="result">执行结果</param>
    Task UpdateRunStatusAsync(int id, bool success, string? result);
}
