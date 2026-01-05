using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectAlert.Domain.Entities;
using ProjectAlert.WPF.Services;
using ProjectAlert.WPF.Services.TaskQueue;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 统计浮窗视图模型
/// </summary>
public partial class StatFloatingViewModel : FloatingWidgetViewModelBase
{
    private readonly ITaskQueue _taskQueue;
    private readonly LogService _logService;

    /// <summary>
    /// 统计配置
    /// </summary>
    [ObservableProperty]
    private StatConfig _statConfig = null!;

    /// <summary>
    /// 统计配置ID
    /// </summary>
    public int StatConfigId => StatConfig?.Id ?? 0;

    /// <summary>
    /// 刷新间隔（秒）
    /// </summary>
    public int RefreshInterval => StatConfig?.RefreshInterval ?? 60;

    /// <summary>
    /// 查询结果数据
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Dictionary<string, object>> _data = [];

    /// <summary>
    /// 列名列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _columns = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    public StatFloatingViewModel(ITaskQueue taskQueue, LogService logService)
    {
        _taskQueue = taskQueue;
        _logService = logService;

        // 订阅任务完成事件
        _taskQueue.TaskCompleted += OnTaskCompleted;
        _taskQueue.TaskStarted += OnTaskStarted;
    }

    /// <summary>
    /// 初始化统计配置
    /// </summary>
    public void Initialize(StatConfig config)
    {
        StatConfig = config;
        Title = config.Name;
    }

    /// <summary>
    /// 窗口显示时调用
    /// </summary>
    /// <param name="initialDelay">初始延迟（用于错开启动）</param>
    public void OnWindowShown(TimeSpan? initialDelay = null)
    {
        // 请求刷新数据（可带初始延迟）
        _taskQueue.Enqueue(new TaskRequest
        {
            Type = TaskType.StatRefresh,
            TargetId = StatConfigId,
            ScheduledTime = initialDelay.HasValue
                ? DateTime.Now.Add(initialDelay.Value)
                : null,
            Priority = 2,
            Source = "统计浮窗显示"
        });

        // 注册定时刷新（带初始延迟，错开执行）
        _taskQueue.RegisterTimer(
            TaskType.StatRefresh,
            targetId: StatConfigId,
            interval: TimeSpan.FromSeconds(RefreshInterval),
            initialDelay: initialDelay
        );
    }

    /// <summary>
    /// 窗口隐藏时调用
    /// </summary>
    public void OnWindowHidden()
    {
        _taskQueue.UnregisterTimer(TaskType.StatRefresh, StatConfigId);
    }

    /// <summary>
    /// 请求刷新数据
    /// </summary>
    public void RequestRefresh(string source = "手动刷新")
    {
        _taskQueue.Enqueue(new TaskRequest
        {
            Type = TaskType.StatRefresh,
            TargetId = StatConfigId,
            Priority = 1,  // 高优先级
            Source = source
        });
    }

    /// <summary>
    /// 任务开始时
    /// </summary>
    private void OnTaskStarted(TaskRequest request)
    {
        // 只处理自己的任务
        if (request.Type != TaskType.StatRefresh) return;
        if (request.TargetId != StatConfigId) return;

        _logService.TaskQueueLog("VM收到开始", request.TaskKey, $"StatFloatingViewModel[{StatConfigId}].OnTaskStarted");
        IsLoading = true;
        ErrorMessage = null;
    }

    /// <summary>
    /// 任务完成时
    /// </summary>
    private void OnTaskCompleted(TaskCompletedEvent e)
    {
        // 只处理统计刷新 + 且是自己的数据
        if (e.Type != TaskType.StatRefresh) return;
        if (e.TargetId != StatConfigId) return;

        _logService.TaskQueueLog("VM收到完成", $"StatRefresh_{StatConfigId}",
            $"Success={e.Success}, 耗时={e.Duration.TotalMilliseconds:F0}ms");

        IsLoading = false;

        if (e.Success && e.Data is StatRefreshResult data)
        {
            _logService.TaskQueueLog("VM应用数据", $"StatRefresh_{StatConfigId}",
                $"行数={data.Rows.Count}, 列数={data.Columns.Count}");
            ApplyRefreshResult(data);
            _logService.TaskQueueLog("VM更新完成", $"StatRefresh_{StatConfigId}", "UI已更新");
        }
        else if (!e.Success)
        {
            _logService.TaskQueueLog("VM处理失败", $"StatRefresh_{StatConfigId}", $"错误={e.ErrorMessage}");
            ErrorMessage = e.ErrorMessage ?? "刷新失败";
        }
    }

    /// <summary>
    /// 应用刷新结果
    /// </summary>
    private void ApplyRefreshResult(StatRefreshResult data)
    {
        // 更新列名
        Columns.Clear();
        foreach (var col in data.Columns)
        {
            Columns.Add(col);
        }

        // 更新数据（使用带变化标记的显示数据）
        Data.Clear();
        foreach (var row in data.DisplayRows)
        {
            Data.Add(row);
        }

        LastUpdateTime = data.UpdateTime;
        ErrorMessage = null;
    }

    /// <summary>
    /// 刷新数据（兼容旧接口，改为通过任务队列）
    /// </summary>
    public override Task RefreshAsync()
    {
        RequestRefresh("RefreshAsync调用");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 取消任务队列事件订阅
            _taskQueue.TaskCompleted -= OnTaskCompleted;
            _taskQueue.TaskStarted -= OnTaskStarted;
        }

        base.Dispose(disposing);
    }
}
