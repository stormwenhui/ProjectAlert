namespace ProjectAlert.WPF.Services.TaskQueue;

/// <summary>
/// 任务队列服务接口
/// </summary>
public interface ITaskQueue
{
    #region 任务提交

    /// <summary>
    /// 提交单次任务
    /// </summary>
    /// <param name="request">任务请求</param>
    void Enqueue(TaskRequest request);

    /// <summary>
    /// 批量提交任务（自动错开执行时间）
    /// </summary>
    /// <param name="requests">任务请求列表</param>
    /// <param name="intervalMs">任务间隔（毫秒），null表示使用随机间隔</param>
    void EnqueueBatch(IEnumerable<TaskRequest> requests, int? intervalMs = null);

    #endregion

    #region 定时任务管理

    /// <summary>
    /// 注册定时任务
    /// </summary>
    /// <param name="type">任务类型</param>
    /// <param name="targetId">目标ID</param>
    /// <param name="interval">执行间隔</param>
    /// <param name="initialDelay">初次执行延迟（用于错开启动）</param>
    void RegisterTimer(TaskType type, int? targetId, TimeSpan interval, TimeSpan? initialDelay = null);

    /// <summary>
    /// 取消定时任务
    /// </summary>
    void UnregisterTimer(TaskType type, int? targetId);

    /// <summary>
    /// 检查定时任务是否已注册
    /// </summary>
    bool IsTimerRegistered(TaskType type, int? targetId);

    #endregion

    #region 任务控制

    /// <summary>
    /// 取消指定任务（从队列中移除）
    /// </summary>
    bool Cancel(string taskKey);

    /// <summary>
    /// 取消所有任务
    /// </summary>
    void CancelAll();

    /// <summary>
    /// 获取队列中的任务数量
    /// </summary>
    int QueueCount { get; }

    /// <summary>
    /// 获取正在执行的任务数量
    /// </summary>
    int RunningCount { get; }

    #endregion

    #region 事件

    /// <summary>
    /// 任务完成事件（UI线程触发）
    /// </summary>
    event Action<TaskCompletedEvent>? TaskCompleted;

    /// <summary>
    /// 任务开始事件
    /// </summary>
    event Action<TaskRequest>? TaskStarted;

    #endregion
}
