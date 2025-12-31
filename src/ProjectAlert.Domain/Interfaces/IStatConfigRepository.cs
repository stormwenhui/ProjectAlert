using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 统计配置仓储接口
/// </summary>
public interface IStatConfigRepository : IRepository<StatConfig>
{
    /// <summary>
    /// 获取所有启用的统计配置
    /// </summary>
    /// <returns>启用的统计配置集合</returns>
    Task<IEnumerable<StatConfig>> GetEnabledAsync();

    /// <summary>
    /// 根据系统分类获取统计配置
    /// </summary>
    /// <param name="category">系统分类</param>
    /// <returns>统计配置集合</returns>
    Task<IEnumerable<StatConfig>> GetByCategoryAsync(SystemCategory category);
}
