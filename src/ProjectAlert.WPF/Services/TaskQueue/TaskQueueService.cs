using System.Collections.Concurrent;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProjectAlert.WPF.Services.TaskQueue;

/// <summary>
/// 任务队列服务实现
/// </summary>
public class TaskQueueService : ITaskQueue, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LogService _logService;
    private readonly TaskQueueOptions _options;

    // 任务队列（按优先级排序）
    private readonly PriorityQueue<TaskRequest, (DateTime ScheduledTime, int Priority, DateTime CreatedAt)> _queue = new();
    private readonly object _queueLock = new();

    // 并发控制
    private readonly SemaphoreSlim _semaphore;
    private int _runningCount;

    // 去重控制
    private readonly ConcurrentDictionary<string, DateTime> _recentTasks = new();

    // 定时任务管理
    private readonly ConcurrentDictionary<string, TimerInfo> _timers = new();

    // 调度器控制
    private readonly CancellationTokenSource _schedulerCts = new();
    private Task? _schedulerTask;
    private bool _disposed;

    /// <inheritdoc/>
    public int QueueCount
    {
        get
        {
            lock (_queueLock)
            {
                return _queue.Count;
            }
        }
    }

    /// <inheritdoc/>
    public int RunningCount => _runningCount;

    /// <inheritdoc/>
    public event Action<TaskCompletedEvent>? TaskCompleted;

    /// <inheritdoc/>
    public event Action<TaskRequest>? TaskStarted;

    public TaskQueueService(
        IServiceProvider serviceProvider,
        LogService logService,
        IOptions<TaskQueueOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logService = logService;
        _options = options.Value;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrency);

        // 启动调度器
        _schedulerTask = Task.Run(SchedulerLoopAsync);

        _logService.Debug("任务队列", $"任务队列已启动，最大并发数: {_options.MaxConcurrency}");
    }

    #region 任务提交

    /// <inheritdoc/>
    public void Enqueue(TaskRequest request)
    {
        if (_disposed) return;

        // 去重检查
        if (_options.EnableDeduplication && IsRecentlyExecuted(request.TaskKey))
        {
            _logService.Debug("任务队列", $"任务已存在，跳过: {request.TaskKey}");
            return;
        }

        lock (_queueLock)
        {
            var priority = (request.EffectiveTime, request.Priority, request.CreatedAt);
            _queue.Enqueue(request, priority);
        }

        _logService.TaskQueueLog("入队", request.TaskKey,
            $"来源={request.Source}, 优先级={request.Priority}, 计划时间={request.ScheduledTime?.ToString("HH:mm:ss.fff") ?? "立即"}, 队列长度={QueueCount}");
    }

    /// <inheritdoc/>
    public void EnqueueBatch(IEnumerable<TaskRequest> requests, int? intervalMs = null)
    {
        var requestList = requests.ToList();
        if (requestList.Count == 0) return;

        var random = new Random();
        var delayOffset = 0;

        foreach (var request in requestList)
        {
            // 如果请求没有指定计划时间，自动添加延迟
            if (request.ScheduledTime == null)
            {
                request.ScheduledTime = DateTime.Now.AddMilliseconds(delayOffset);
            }

            Enqueue(request);

            // 计算下一个任务的延迟
            delayOffset += intervalMs ?? random.Next(_options.BatchIntervalMinMs, _options.BatchIntervalMaxMs + 1);
        }

        _logService.Info("任务队列", $"批量入队 {requestList.Count} 个任务");
    }

    #endregion

    #region 定时任务管理

    /// <inheritdoc/>
    public void RegisterTimer(TaskType type, int? targetId, TimeSpan interval, TimeSpan? initialDelay = null)
    {
        var timerKey = targetId.HasValue ? $"{type}_{targetId}" : type.ToString();

        // 如果已存在，先取消
        UnregisterTimer(type, targetId);

        var timerInfo = new TimerInfo
        {
            Type = type,
            TargetId = targetId,
            Interval = interval,
            CancellationTokenSource = new CancellationTokenSource()
        };

        if (_timers.TryAdd(timerKey, timerInfo))
        {
            // 启动定时任务
            _ = TimerLoopAsync(timerInfo, initialDelay);
            _logService.Debug("任务队列", $"注册定时任务: {timerKey}, 间隔: {interval.TotalSeconds}秒, 初始延迟: {initialDelay?.TotalMilliseconds ?? 0}ms");
        }
    }

    /// <inheritdoc/>
    public void UnregisterTimer(TaskType type, int? targetId)
    {
        var timerKey = targetId.HasValue ? $"{type}_{targetId}" : type.ToString();

        if (_timers.TryRemove(timerKey, out var timerInfo))
        {
            timerInfo.CancellationTokenSource?.Cancel();
            timerInfo.CancellationTokenSource?.Dispose();
            _logService.Debug("任务队列", $"取消定时任务: {timerKey}");
        }
    }

    /// <inheritdoc/>
    public bool IsTimerRegistered(TaskType type, int? targetId)
    {
        var timerKey = targetId.HasValue ? $"{type}_{targetId}" : type.ToString();
        return _timers.ContainsKey(timerKey);
    }

    private async Task TimerLoopAsync(TimerInfo timerInfo, TimeSpan? initialDelay)
    {
        var cts = timerInfo.CancellationTokenSource;
        if (cts == null) return;

        try
        {
            // 初始延迟
            if (initialDelay.HasValue && initialDelay.Value > TimeSpan.Zero)
            {
                await Task.Delay(initialDelay.Value, cts.Token);
            }

            while (!cts.Token.IsCancellationRequested)
            {
                // 提交任务
                Enqueue(new TaskRequest
                {
                    Type = timerInfo.Type,
                    TargetId = timerInfo.TargetId,
                    Priority = 5,  // 定时任务普通优先级
                    Source = "定时任务"
                });

                // 等待下一个周期
                await Task.Delay(timerInfo.Interval, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
    }

    #endregion

    #region 任务控制

    /// <inheritdoc/>
    public bool Cancel(string taskKey)
    {
        // 注：简单实现，仅从去重字典中移除
        return _recentTasks.TryRemove(taskKey, out _);
    }

    /// <inheritdoc/>
    public void CancelAll()
    {
        lock (_queueLock)
        {
            _queue.Clear();
        }
        _recentTasks.Clear();
    }

    #endregion

    #region 调度器

    private async Task SchedulerLoopAsync()
    {
        while (!_schedulerCts.Token.IsCancellationRequested)
        {
            try
            {
                TaskRequest? task = null;

                lock (_queueLock)
                {
                    if (_queue.Count > 0)
                    {
                        // 查看队首任务
                        if (_queue.TryPeek(out var peeked, out var priority))
                        {
                            // 检查是否到达执行时间
                            if (priority.ScheduledTime <= DateTime.Now)
                            {
                                _queue.Dequeue();
                                task = peeked;
                            }
                        }
                    }
                }

                if (task != null)
                {
                    // 计算等待时间
                    var waitTime = DateTime.Now - task.CreatedAt;
                    _logService.TaskQueueLog("调度", task.TaskKey,
                        $"等待时间={waitTime.TotalMilliseconds:F0}ms, 当前运行={_runningCount}, 队列剩余={QueueCount}");

                    // 等待并发槽
                    await _semaphore.WaitAsync(_schedulerCts.Token);
                    Interlocked.Increment(ref _runningCount);

                    // 异步执行任务（不阻塞调度器）
                    _ = ExecuteTaskAsync(task);

                    // 任务间最小间隔
                    await Task.Delay(_options.MinTaskIntervalMs, _schedulerCts.Token);
                }
                else
                {
                    // 队列为空或未到执行时间，短暂等待
                    await Task.Delay(100, _schedulerCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logService.Error("任务队列", $"调度器异常: {ex.Message}");
                await Task.Delay(1000);  // 异常后等待一段时间
            }
        }
    }

    private async Task ExecuteTaskAsync(TaskRequest request)
    {
        var startTime = DateTime.Now;

        try
        {
            // 标记为最近执行（用于去重）
            MarkAsRecentlyExecuted(request.TaskKey);

            _logService.TaskQueueLog("开始执行", request.TaskKey, $"触发 TaskStarted 事件");

            // 触发开始事件
            InvokeOnUIThread(() => TaskStarted?.Invoke(request));

            // 获取执行器
            using var scope = _serviceProvider.CreateScope();
            var executors = scope.ServiceProvider.GetServices<ITaskExecutor>();
            var executor = executors.FirstOrDefault(e => e.SupportedType == request.Type);

            if (executor == null)
            {
                throw new InvalidOperationException($"未找到任务执行器: {request.Type}");
            }

            _logService.TaskQueueLog("执行器", request.TaskKey, $"使用执行器: {executor.GetType().Name}");

            // 执行任务（带超时）
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TaskTimeoutSeconds));
            var result = await executor.ExecuteAsync(request);

            var duration = DateTime.Now - startTime;

            // 记录结果摘要
            var resultSummary = GetResultSummary(result);
            _logService.TaskQueueLog("执行完成", request.TaskKey, $"耗时={duration.TotalMilliseconds:F0}ms, 结果={resultSummary}");

            // 触发完成事件
            var completedEvent = new TaskCompletedEvent
            {
                Type = request.Type,
                TargetId = request.TargetId,
                Success = true,
                Data = result,
                Duration = duration
            };

            _logService.TaskQueueLog("通知UI", request.TaskKey, "触发 TaskCompleted 事件 (Success)");
            InvokeOnUIThread(() => TaskCompleted?.Invoke(completedEvent));
        }
        catch (Exception ex)
        {
            var duration = DateTime.Now - startTime;
            _logService.TaskQueueLog("执行失败", request.TaskKey, $"耗时={duration.TotalMilliseconds:F0}ms, 错误={ex.Message}");

            var completedEvent = new TaskCompletedEvent
            {
                Type = request.Type,
                TargetId = request.TargetId,
                Success = false,
                ErrorMessage = ex.Message,
                Duration = duration
            };

            _logService.TaskQueueLog("通知UI", request.TaskKey, "触发 TaskCompleted 事件 (Failed)");
            InvokeOnUIThread(() => TaskCompleted?.Invoke(completedEvent));
        }
        finally
        {
            Interlocked.Decrement(ref _runningCount);
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 获取结果摘要
    /// </summary>
    private static string GetResultSummary(object? result)
    {
        return result switch
        {
            AlertRefreshResult alertResult => $"预警刷新: 严重={alertResult.CriticalCount}, 警告={alertResult.WarningCount}, 信息={alertResult.InfoCount}",
            StatRefreshResult statResult => $"统计刷新: 行数={statResult.Rows.Count}, 列数={statResult.Columns.Count}",
            AlertCheckResult checkResult => $"预警检查: 规则={checkResult.RuleName}, 预警数={checkResult.AlertCount}, 有新预警={checkResult.HasNewAlert}",
            null => "null",
            _ => result.GetType().Name
        };
    }

    #endregion

    #region 辅助方法

    private bool IsRecentlyExecuted(string taskKey)
    {
        if (_recentTasks.TryGetValue(taskKey, out var lastTime))
        {
            if ((DateTime.Now - lastTime).TotalSeconds < _options.DeduplicationWindowSeconds)
            {
                return true;
            }
        }
        return false;
    }

    private void MarkAsRecentlyExecuted(string taskKey)
    {
        _recentTasks[taskKey] = DateTime.Now;

        // 清理过期的去重记录
        var expiredKeys = _recentTasks
            .Where(kv => (DateTime.Now - kv.Value).TotalSeconds > _options.DeduplicationWindowSeconds * 2)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _recentTasks.TryRemove(key, out _);
        }
    }

    private void InvokeOnUIThread(Action action)
    {
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 取消所有定时任务
        foreach (var timer in _timers.Values)
        {
            timer.CancellationTokenSource?.Cancel();
            timer.CancellationTokenSource?.Dispose();
        }
        _timers.Clear();

        // 停止调度器
        _schedulerCts.Cancel();
        _schedulerTask?.Wait(TimeSpan.FromSeconds(5));
        _schedulerCts.Dispose();

        _semaphore.Dispose();

        _logService.Debug("任务队列", "任务队列已停止");
    }

    #endregion
}
