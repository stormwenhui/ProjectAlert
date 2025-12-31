using Dapper.Contrib.Extensions;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 当前预警
/// </summary>
[Table("current_alerts")]
public class CurrentAlert : BaseEntity
{
    /// <summary>
    /// 规则ID
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// 预警唯一标识（多行预警时为 key_field 值）
    /// </summary>
    public string? AlertKey { get; set; }

    /// <summary>
    /// 预警消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 预警级别
    /// </summary>
    public AlertLevel AlertLevel { get; set; }

    /// <summary>
    /// 处理状态
    /// </summary>
    public AlertStatus Status { get; set; } = AlertStatus.未处理;

    /// <summary>
    /// 首次触发时间
    /// </summary>
    public DateTime FirstTime { get; set; }

    /// <summary>
    /// 最后异常时间
    /// </summary>
    public DateTime LastTime { get; set; }

    /// <summary>
    /// 累计异常次数
    /// </summary>
    public int OccurCount { get; set; } = 1;

    #region 导航属性

    /// <summary>
    /// 关联的预警规则
    /// </summary>
    [Write(false)]
    public AlertRule? Rule { get; set; }

    #endregion
}
