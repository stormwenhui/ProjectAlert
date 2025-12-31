using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Shared;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 预警规则仓储接口
/// </summary>
public interface IAlertRuleRepository : IRepository<AlertRule>
{
    /// <summary>
    /// 分页搜索预警规则
    /// </summary>
    /// <param name="keyword">搜索关键词（名称）</param>
    /// <param name="category">系统分类</param>
    /// <param name="sourceType">数据源类型</param>
    /// <param name="alertLevel">预警级别</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<AlertRule>> SearchAsync(string? keyword, SystemCategory? category, SourceType? sourceType, AlertLevel? alertLevel, int page, int pageSize);

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
