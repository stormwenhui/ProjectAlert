using ProjectAlert.Domain.Entities;

namespace ProjectAlert.WPF.Services.TaskQueue;

/// <summary>
/// 预警刷新结果数据
/// </summary>
public class AlertRefreshResult
{
    /// <summary>
    /// 预警列表（含规则信息）
    /// </summary>
    public List<CurrentAlert> Alerts { get; set; } = [];

    /// <summary>
    /// 严重预警数量
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// 警告预警数量
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// 信息预警数量
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 统计刷新结果数据
/// </summary>
public class StatRefreshResult
{
    /// <summary>
    /// 统计配置ID
    /// </summary>
    public int StatConfigId { get; set; }

    /// <summary>
    /// 列名列表
    /// </summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// 数据行（原始数据，不含变化标记）
    /// </summary>
    public List<Dictionary<string, object>> Rows { get; set; } = [];

    /// <summary>
    /// 显示数据行（含变化标记）
    /// </summary>
    public List<Dictionary<string, object>> DisplayRows { get; set; } = [];

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 预警检查结果数据
/// </summary>
public class AlertCheckResult
{
    /// <summary>
    /// 规则ID
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 检测到的预警数量
    /// </summary>
    public int AlertCount { get; set; }

    /// <summary>
    /// 是否有新增预警
    /// </summary>
    public bool HasNewAlert { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}
