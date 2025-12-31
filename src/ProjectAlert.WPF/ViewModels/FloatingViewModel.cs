using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

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
    private readonly ICurrentAlertRepository _currentAlertRepository;
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
    public FloatingViewModel(ICurrentAlertRepository currentAlertRepository)
    {
        _currentAlertRepository = currentAlertRepository;
        Title = "项目监控";
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
        await RefreshAsync();
    }

    /// <summary>
    /// 忽略预警
    /// </summary>
    [RelayCommand]
    private async Task IgnoreAlertAsync(AlertDisplayItem? item)
    {
        if (item == null) return;
        await _currentAlertRepository.UpdateStatusAsync(item.Alert.Id, AlertStatus.已忽略, "用户操作");
        await RefreshAsync();
    }

    /// <summary>
    /// 标记为已恢复
    /// </summary>
    [RelayCommand]
    private async Task MarkAsRecoveredAsync(AlertDisplayItem? item)
    {
        if (item == null) return;
        await _currentAlertRepository.UpdateStatusAsync(item.Alert.Id, AlertStatus.已恢复, "用户操作");
        await RefreshAsync();
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
    /// 刷新预警统计
    /// </summary>
    public override async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var alerts = await _currentAlertRepository.GetAllWithRuleAsync();
            // 过滤掉已忽略的预警
            _allAlerts = alerts.Where(a => a.Status != AlertStatus.已忽略).ToList();

            CriticalCount = _allAlerts.Count(a => a.AlertLevel == AlertLevel.严重);
            WarningCount = _allAlerts.Count(a => a.AlertLevel == AlertLevel.警告);
            InfoCount = _allAlerts.Count(a => a.AlertLevel == AlertLevel.信息);
            LastUpdateTime = DateTime.Now;

            OnPropertyChanged(nameof(IsAllClear));

            // 更新分类Tab和分组
            UpdateCategories();
            UpdateAlertGroups();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 更新分类Tab
    /// </summary>
    private void UpdateCategories()
    {
        var categoryGroups = _allAlerts
            .Where(a => a.Rule != null)
            .GroupBy(a => a.Rule!.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        Categories.Clear();

        // 添加"全部"Tab
        Categories.Add(new CategoryTabItem
        {
            Category = null,
            Name = "全部",
            Count = _allAlerts.Count,
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
        // 根据选中的分类筛选
        var filtered = SelectedCategory?.Category == null
            ? _allAlerts
            : _allAlerts.Where(a => a.Rule?.Category == SelectedCategory.Category).ToList();

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
}
