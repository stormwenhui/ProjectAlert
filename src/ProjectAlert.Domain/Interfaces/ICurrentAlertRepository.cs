using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 当前预警仓储接口
/// </summary>
public interface ICurrentAlertRepository : IRepository<CurrentAlert>
{
    /// <summary>
    /// 根据规则ID和预警Key获取预警
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="alertKey">预警唯一标识</param>
    /// <returns>当前预警对象</returns>
    Task<CurrentAlert?> GetByRuleAndKeyAsync(int ruleId, string? alertKey);

    /// <summary>
    /// 获取所有活跃的预警
    /// </summary>
    /// <returns>活跃预警集合</returns>
    Task<IEnumerable<CurrentAlert>> GetActiveAlertsAsync();

    /// <summary>
    /// 根据规则ID获取所有预警
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <returns>预警集合</returns>
    Task<IEnumerable<CurrentAlert>> GetByRuleIdAsync(int ruleId);

    /// <summary>
    /// 更新预警状态
    /// </summary>
    /// <param name="id">预警ID</param>
    /// <param name="status">新状态</param>
    /// <param name="changedBy">变更来源</param>
    Task UpdateStatusAsync(int id, AlertStatus status, string changedBy);

    /// <summary>
    /// 新增或更新预警（存在则更新，不存在则新增）
    /// </summary>
    /// <param name="alert">预警对象</param>
    Task UpsertAlertAsync(CurrentAlert alert);

    /// <summary>
    /// 移除已恢复的预警
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="activeKeys">当前活跃的预警Key集合</param>
    Task RemoveRecoveredAlertsAsync(int ruleId, IEnumerable<string?> activeKeys);

    /// <summary>
    /// 获取所有预警并关联规则信息
    /// </summary>
    /// <returns>包含规则信息的预警集合</returns>
    Task<IEnumerable<CurrentAlert>> GetAllWithRuleAsync();
}
