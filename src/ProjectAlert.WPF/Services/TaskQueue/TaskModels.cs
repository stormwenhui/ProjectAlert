namespace ProjectAlert.WPF.Services.TaskQueue;

/// <summary>
/// 任务类型
/// </summary>
public enum TaskType
{
    /// <summary>
    /// 预警规则检查（执行SQL/API，检测预警条件）
    /// </summary>
    AlertCheck,

    /// <summary>
    /// 预警数据刷新（查询当前预警列表）
    /// </summary>
    AlertRefresh,

    /// <summary>
    /// 统计数据刷新（执行统计查询）
    /// </summary>
    StatRefresh
}

/// <summary>
/// 任务请求
/// </summary>
public class TaskRequest
{
    /// <summary>
    /// 唯一标识（用于去重，格式：{Type}_{TargetId}）
    /// </summary>
    public string TaskKey => TargetId.HasValue
        ? $"{Type}_{TargetId}"
        : Type.ToString();

    /// <summary>
    /// 任务类型
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// 目标ID
    /// - AlertCheck: RuleId（规则ID）
    /// - AlertRefresh: null（预警浮窗唯一）
    /// - StatRefresh: StatConfigId（统计配置ID）
    /// </summary>
    public int? TargetId { get; set; }

    /// <summary>
    /// 计划执行时间（null 表示立即执行）
    /// 用于错开启动时的任务执行
    /// </summary>
    public DateTime? ScheduledTime { get; set; }

    /// <summary>
    /// 优先级（1-10，数字越小优先级越高）
    /// 1-3: 高优先级（用户手动触发）
    /// 4-6: 普通优先级（定时任务）
    /// 7-10: 低优先级（后台任务）
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 请求来源（用于日志追踪）
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取实际执行时间（用于排序）
    /// </summary>
    public DateTime EffectiveTime => ScheduledTime ?? CreatedAt;
}

/// <summary>
/// 任务完成事件
/// </summary>
public class TaskCompletedEvent
{
    /// <summary>
    /// 任务类型
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// 目标ID（浮窗用于判断是否是自己的数据）
    /// </summary>
    public int? TargetId { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 结果数据（携带完整数据，浮窗直接使用）
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 执行耗时
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 定时任务信息
/// </summary>
public class TimerInfo
{
    /// <summary>
    /// 任务类型
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// 目标ID
    /// </summary>
    public int? TargetId { get; set; }

    /// <summary>
    /// 执行间隔
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// 定时器Key
    /// </summary>
    public string TimerKey => TargetId.HasValue
        ? $"{Type}_{TargetId}"
        : Type.ToString();

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }
}

/// <summary>
/// 任务队列配置
/// </summary>
public class TaskQueueOptions
{
    /// <summary>
    /// 最大并发任务数
    /// </summary>
    public int MaxConcurrency { get; set; } = 3;

    /// <summary>
    /// 任务间最小间隔（毫秒）
    /// </summary>
    public int MinTaskIntervalMs { get; set; } = 200;

    /// <summary>
    /// 批量任务默认间隔范围（毫秒）
    /// </summary>
    public int BatchIntervalMinMs { get; set; } = 500;
    public int BatchIntervalMaxMs { get; set; } = 2000;

    /// <summary>
    /// 任务超时时间（秒）
    /// </summary>
    public int TaskTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用任务去重
    /// </summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>
    /// 去重时间窗口（秒）- 同一任务在此时间内不重复执行
    /// </summary>
    public int DeduplicationWindowSeconds { get; set; } = 5;
}
