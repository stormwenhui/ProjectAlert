using Dapper.Contrib.Extensions;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 忽略的预警实体
/// </summary>
[Table("ignored_alerts")]
public class IgnoredAlert
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 规则ID
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// 被忽略的预警 Key
    /// </summary>
    public string? AlertKey { get; set; }

    /// <summary>
    /// 忽略时间
    /// </summary>
    public DateTime IgnoredAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 忽略原因
    /// </summary>
    public string? IgnoredReason { get; set; }
}
