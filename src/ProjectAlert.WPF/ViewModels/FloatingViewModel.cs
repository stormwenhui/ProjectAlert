using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Services;
using ProjectAlert.WPF.Services.TaskQueue;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 预警浮窗显示项（用于按级别分组）
/// </summary>
public partial class AlertDisplayItem : ObservableObject
{
    /// <summary>
    /// 原始预警数据
    /// </summary>
    public CurrentAlert Alert { get; set; } = null!;

    /// <summary>
    /// 来源系统名称
    /// </summary>
    public string SourceName => Alert.Rule?.Category.ToString() ?? "未知";

    /// <summary>
    /// 规则名称
    /// </summary>
    public string RuleName => Alert.Rule?.Name ?? "未知规则";

    /// <summary>
    /// 预警消息
    /// </summary>
    public string Message => Alert.Message;

    /// <summary>
    /// 单行显示的消息（过滤换行符）
    /// </summary>
    public string MessageSingleLine => Alert.Message.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

    /// <summary>
    /// 预警级别
    /// </summary>
    public AlertLevel AlertLevel => Alert.AlertLevel;

    /// <summary>
    /// 处理状态
    /// </summary>
    public AlertStatus Status => Alert.Status;

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Alert.Status switch
    {
        AlertStatus.处理中 => "处理中",
        AlertStatus.已忽略 => "已忽略",
        AlertStatus.已恢复 => "已恢复",
        _ => ""
    };

    /// <summary>
    /// 是否显示状态标签
    /// </summary>
    public bool ShowStatus => Alert.Status != AlertStatus.未处理;

    /// <summary>
    /// 首次触发时间
    /// </summary>
    public string FirstTimeText => Alert.FirstTime.ToString("HH:mm");

    /// <summary>
    /// 最后触发时间
    /// </summary>
    public string LastTimeText => Alert.LastTime.ToString("HH:mm");

    /// <summary>
    /// 累计次数
    /// </summary>
    public int OccurCount => Alert.OccurCount;

    /// <summary>
    /// 元信息文本
    /// </summary>
    public string MetaText => $"首次: {FirstTimeText} | 最后: {LastTimeText} | 累计 {OccurCount}次";
}

/// <summary>
/// 预警分组（按级别）
/// </summary>
public partial class AlertGroup : ObservableObject
{
    /// <summary>
    /// 级别
    /// </summary>
    public AlertLevel Level { get; set; }

    /// <summary>
    /// 级别名称
    /// </summary>
    public string LevelName => Level.ToString();

    /// <summary>
    /// 数量
    /// </summary>
    [ObservableProperty]
    private int _count;

    /// <summary>
    /// 预警项集合
    /// </summary>
    public ObservableCollection<AlertDisplayItem> Items { get; } = [];
}

/// <summary>
/// 分类Tab项
/// </summary>
public partial class CategoryTabItem : ObservableObject
{
    /// <summary>
    /// 分类值（null表示全部）
    /// </summary>
    public SystemCategory? Category { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数量
    /// </summary>
    [ObservableProperty]
    private int _count;

    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// 预警悬浮窗视图模型
/// </summary>
public partial class FloatingViewModel : FloatingWidgetViewModelBase
{
    private readonly ITaskQueue _taskQueue;
    private readonly ICurrentAlertRepository _currentAlertRepository;
    private readonly LogService _logService;
    private List<CurrentAlert> _allAlerts = [];

    /// <summary>
    /// 严重预警数量
    /// </summary>
    [ObservableProperty]
    private int _criticalCount;

    /// <summary>
    /// 警告预警数量
    /// </summary>
    [ObservableProperty]
    private int _warningCount;

    /// <summary>
    /// 信息预警数量
    /// </summary>
    [ObservableProperty]
    private int _infoCount;

    /// <summary>
    /// 分类Tab列表
    /// </summary>
    public ObservableCollection<CategoryTabItem> Categories { get; } = [];

    /// <summary>
    /// 当前选中的分类
    /// </summary>
    [ObservableProperty]
    private CategoryTabItem? _selectedCategory;

    /// <summary>
    /// 严重级别预警分组
    /// </summary>
    [ObservableProperty]
    private AlertGroup? _criticalGroup;

    /// <summary>
    /// 警告级别预警分组
    /// </summary>
    [ObservableProperty]
    private AlertGroup? _warningGroup;

    /// <summary>
    /// 信息级别预警分组
    /// </summary>
    [ObservableProperty]
    private AlertGroup? _infoGroup;

    /// <summary>
    /// 是否一切正常（没有预警）
    /// </summary>
    public bool IsAllClear => CriticalCount == 0 && WarningCount == 0 && InfoCount == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FloatingViewModel(ITaskQueue taskQueue, ICurrentAlertRepository currentAlertRepository, LogService logService)
    {
        _taskQueue = taskQueue;
        _currentAlertRepository = currentAlertRepository;
        _logService = logService;
        Title = "项目监控";

        // 订阅任务完成事件
        _taskQueue.TaskCompleted += OnTaskCompleted;
        _taskQueue.TaskStarted += OnTaskStarted;
    }

    /// <summary>
    /// 窗口显示时调用
    /// </summary>
    public void OnWindowShown()
    {
        // 请求刷新数据
        RequestRefresh("窗口显示");

        // 注册定时刷新（30秒间隔）
        _taskQueue.RegisterTimer(
            TaskType.AlertRefresh,
            targetId: null,
            interval: TimeSpan.FromSeconds(30)
        );
    }

    /// <summary>
    /// 窗口隐藏时调用
    /// </summary>
    public void OnWindowHidden()
    {
        _taskQueue.UnregisterTimer(TaskType.AlertRefresh, targetId: null);
    }

    /// <summary>
    /// 请求刷新数据
    /// </summary>
    public void RequestRefresh(string source = "手动刷新")
    {
        _taskQueue.Enqueue(new TaskRequest
        {
            Type = TaskType.AlertRefresh,
            Priority = 1,  // 高优先级
            Source = source
        });
    }

    /// <summary>
    /// 任务开始时
    /// </summary>
    private void OnTaskStarted(TaskRequest request)
    {
        if (request.Type != TaskType.AlertRefresh) return;

        _logService.TaskQueueLog("VM收到开始", request.TaskKey, "FloatingViewModel.OnTaskStarted");
        IsLoading = true;
        ErrorMessage = null;
    }

    /// <summary>
    /// 任务完成时
    /// </summary>
    private void OnTaskCompleted(TaskCompletedEvent e)
    {
        // 处理预警刷新结果
        if (e.Type == TaskType.AlertRefresh)
        {
            _logService.TaskQueueLog("VM收到完成", "AlertRefresh",
                $"Success={e.Success}, 耗时={e.Duration.TotalMilliseconds:F0}ms");

            IsLoading = false;

            if (e.Success && e.Data is AlertRefreshResult data)
            {
                _logService.TaskQueueLog("VM应用数据", "AlertRefresh",
                    $"预警数={data.Alerts.Count}, 严重={data.CriticalCount}, 警告={data.WarningCount}, 信息={data.InfoCount}");
                ApplyRefreshResult(data);
                _logService.TaskQueueLog("VM更新完成", "AlertRefresh", "UI已更新");
            }
            else if (!e.Success)
            {
                _logService.TaskQueueLog("VM处理失败", "AlertRefresh", $"错误={e.ErrorMessage}");
                ErrorMessage = e.ErrorMessage ?? "刷新失败";
            }
        }
        // 处理预警检查结果（有新预警时刷新界面）
        else if (e.Type == TaskType.AlertCheck && e.Success && e.Data is AlertCheckResult checkResult)
        {
            if (checkResult.HasNewAlert)
            {
                _logService.TaskQueueLog("VM新预警通知", $"AlertCheck_{checkResult.RuleId}",
                    $"规则={checkResult.RuleName}, 触发刷新请求");
                // 有新预警，请求刷新数据
                RequestRefresh("新预警通知");
            }
        }
    }

    /// <summary>
    /// 应用刷新结果
    /// </summary>
    private void ApplyRefreshResult(AlertRefreshResult data)
    {
        _allAlerts = data.Alerts;

        // 只统计未处理的预警数量
        var unhandledAlerts = _allAlerts.Where(a => a.Status == AlertStatus.未处理).ToList();
        CriticalCount = unhandledAlerts.Count(a => a.AlertLevel == AlertLevel.严重);
        WarningCount = unhandledAlerts.Count(a => a.AlertLevel == AlertLevel.警告);
        InfoCount = unhandledAlerts.Count(a => a.AlertLevel == AlertLevel.信息);

        LastUpdateTime = data.UpdateTime;
        ErrorMessage = null;

        OnPropertyChanged(nameof(IsAllClear));

        // 更新分类Tab和分组
        UpdateCategories();
        UpdateAlertGroups();
    }

    /// <summary>
    /// 选择分类
    /// </summary>
    [RelayCommand]
    private void SelectCategory(CategoryTabItem? category)
    {
        if (category == null) return;

        foreach (var cat in Categories)
        {
            cat.IsSelected = cat == category;
        }
        SelectedCategory = category;
        UpdateAlertGroups();
    }

    /// <summary>
    /// 标记为处理中
    /// </summary>
    [RelayCommand]
    private async Task MarkAsProcessingAsync(AlertDisplayItem? item)
    {
        if (item == null) return;
        await _currentAlertRepository.UpdateStatusAsync(item.Alert.Id, AlertStatus.处理中, "用户操作");
        RequestRefresh("状态更新");
    }

    /// <summary>
    /// 忽略预警
    /// </summary>
    [RelayCommand]
    private async Task IgnoreAlertAsync(AlertDisplayItem? item)
    {
        if (item == null) return;
        await _currentAlertRepository.UpdateStatusAsync(item.Alert.Id, AlertStatus.已忽略, "用户操作");
        RequestRefresh("状态更新");
    }

    /// <summary>
    /// 标记为已恢复
    /// </summary>
    [RelayCommand]
    private async Task MarkAsRecoveredAsync(AlertDisplayItem? item)
    {
        if (item == null) return;
        await _currentAlertRepository.UpdateStatusAsync(item.Alert.Id, AlertStatus.已恢复, "用户操作");
        RequestRefresh("状态更新");
    }

    /// <summary>
    /// 复制消息
    /// </summary>
    [RelayCommand]
    private void CopyMessage(AlertDisplayItem? item)
    {
        if (item == null) return;
        try
        {
            System.Windows.Clipboard.SetText(item.Message);
        }
        catch
        {
            // 忽略剪贴板错误
        }
    }

    /// <summary>
    /// 显示消息详情弹窗
    /// </summary>
    [RelayCommand]
    private void ShowMessageDetail(AlertDisplayItem? item)
    {
        if (item == null) return;
        System.Windows.MessageBox.Show(
            item.Message,
            $"预警详情 - {item.SourceName}",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 刷新预警统计（兼容旧接口，改为通过任务队列）
    /// </summary>
    public override Task RefreshAsync()
    {
        RequestRefresh("RefreshAsync调用");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新分类Tab
    /// </summary>
    private void UpdateCategories()
    {
        // 只统计未处理的预警
        var unhandledAlerts = _allAlerts.Where(a => a.Status == AlertStatus.未处理).ToList();

        var categoryGroups = unhandledAlerts
            .Where(a => a.Rule != null)
            .GroupBy(a => a.Rule!.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        Categories.Clear();

        // 添加"全部"Tab
        Categories.Add(new CategoryTabItem
        {
            Category = null,
            Name = "全部",
            Count = unhandledAlerts.Count,
            IsSelected = SelectedCategory?.Category == null
        });

        // 添加各分类Tab
        foreach (var category in Enum.GetValues<SystemCategory>())
        {
            if (categoryGroups.TryGetValue(category, out var count) && count > 0)
            {
                Categories.Add(new CategoryTabItem
                {
                    Category = category,
                    Name = category.ToString(),
                    Count = count,
                    IsSelected = SelectedCategory?.Category == category
                });
            }
        }

        // 如果没有选中项，选中"全部"
        if (SelectedCategory == null || Categories.All(c => !c.IsSelected))
        {
            var allTab = Categories.FirstOrDefault();
            if (allTab != null)
            {
                allTab.IsSelected = true;
                SelectedCategory = allTab;
            }
        }
    }

    /// <summary>
    /// 更新预警分组
    /// </summary>
    private void UpdateAlertGroups()
    {
        // 只显示未处理的预警，根据选中的分类筛选
        var filtered = _allAlerts
            .Where(a => a.Status == AlertStatus.未处理)
            .Where(a => SelectedCategory?.Category == null || a.Rule?.Category == SelectedCategory.Category)
            .ToList();

        // 严重级别分组
        var criticalItems = filtered.Where(a => a.AlertLevel == AlertLevel.严重).ToList();
        if (criticalItems.Count > 0)
        {
            CriticalGroup = new AlertGroup { Level = AlertLevel.严重, Count = criticalItems.Count };
            CriticalGroup.Items.Clear();
            foreach (var alert in criticalItems)
            {
                CriticalGroup.Items.Add(new AlertDisplayItem { Alert = alert });
            }
        }
        else
        {
            CriticalGroup = null;
        }

        // 警告级别分组
        var warningItems = filtered.Where(a => a.AlertLevel == AlertLevel.警告).ToList();
        if (warningItems.Count > 0)
        {
            WarningGroup = new AlertGroup { Level = AlertLevel.警告, Count = warningItems.Count };
            WarningGroup.Items.Clear();
            foreach (var alert in warningItems)
            {
                WarningGroup.Items.Add(new AlertDisplayItem { Alert = alert });
            }
        }
        else
        {
            WarningGroup = null;
        }

        // 信息级别分组
        var infoItems = filtered.Where(a => a.AlertLevel == AlertLevel.信息).ToList();
        if (infoItems.Count > 0)
        {
            InfoGroup = new AlertGroup { Level = AlertLevel.信息, Count = infoItems.Count };
            InfoGroup.Items.Clear();
            foreach (var alert in infoItems)
            {
                InfoGroup.Items.Add(new AlertDisplayItem { Alert = alert });
            }
        }
        else
        {
            InfoGroup = null;
        }
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
