using ProjectAlert.Domain.Entities;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 忽略预警仓储接口
/// </summary>
public interface IIgnoredAlertRepository
{
    /// <summary>
    /// 获取所有忽略的预警
    /// </summary>
    /// <returns>忽略预警集合</returns>
    Task<IEnumerable<IgnoredAlert>> GetAllAsync();

    /// <summary>
    /// 根据规则ID获取忽略的预警
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <returns>忽略预警集合</returns>
    Task<IEnumerable<IgnoredAlert>> GetByRuleIdAsync(int ruleId);

    /// <summary>
    /// 判断指定预警是否被忽略
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="alertKey">预警唯一标识</param>
    /// <returns>是否被忽略</returns>
    Task<bool> IsIgnoredAsync(int ruleId, string? alertKey);

    /// <summary>
    /// 添加忽略预警记录
    /// </summary>
    /// <param name="ignoredAlert">忽略预警对象</param>
    Task AddAsync(IgnoredAlert ignoredAlert);

    /// <summary>
    /// 根据ID移除忽略记录
    /// </summary>
    /// <param name="id">记录ID</param>
    Task RemoveAsync(int id);

    /// <summary>
    /// 根据规则ID和预警Key移除忽略记录
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="alertKey">预警唯一标识</param>
    Task RemoveByRuleAndKeyAsync(int ruleId, string? alertKey);
}
