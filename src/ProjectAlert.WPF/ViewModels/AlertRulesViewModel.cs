using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Views;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 预警规则视图模型
/// </summary>
public partial class AlertRulesViewModel : ObservableObject
{
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IDbConnectionRepository _dbConnectionRepository;

    /// <summary>
    /// 规则列表
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<AlertRule> _rules = new();

    /// <summary>
    /// 选中的规则
    /// </summary>
    [ObservableProperty]
    private AlertRule? _selectedRule;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private bool _isLoading;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    private string? _searchKeyword;

    /// <summary>
    /// 选中的分类
    /// </summary>
    [ObservableProperty]
    private SystemCategory? _selectedCategory;

    /// <summary>
    /// 选中的数据源类型
    /// </summary>
    [ObservableProperty]
    private SourceType? _selectedSourceType;

    /// <summary>
    /// 选中的预警级别
    /// </summary>
    [ObservableProperty]
    private AlertLevel? _selectedAlertLevel;

    /// <summary>
    /// 分类列表
    /// </summary>
    public IEnumerable<SystemCategory?> CategoryOptions { get; } =
        new SystemCategory?[] { null }.Concat(Enum.GetValues<SystemCategory>().Cast<SystemCategory?>());

    /// <summary>
    /// 数据源类型列表
    /// </summary>
    public IEnumerable<SourceType?> SourceTypeOptions { get; } =
        new SourceType?[] { null }.Concat(Enum.GetValues<SourceType>().Cast<SourceType?>());

    /// <summary>
    /// 预警级别列表
    /// </summary>
    public IEnumerable<AlertLevel?> AlertLevelOptions { get; } =
        new AlertLevel?[] { null }.Concat(Enum.GetValues<AlertLevel>().Cast<AlertLevel?>());

    /// <summary>
    /// 当前页码
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    private int _currentPage = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    [ObservableProperty]
    private int _pageSize = 15;

    /// <summary>
    /// 总记录数
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    private int _totalCount;

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 是否没有数据
    /// </summary>
    public bool HasNoData => !IsLoading && Rules.Count == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="alertRuleRepository">预警规则仓储</param>
    /// <param name="dbConnectionRepository">数据库连接仓储</param>
    public AlertRulesViewModel(
        IAlertRuleRepository alertRuleRepository,
        IDbConnectionRepository dbConnectionRepository)
    {
        _alertRuleRepository = alertRuleRepository;
        _dbConnectionRepository = dbConnectionRepository;
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _alertRuleRepository.SearchAsync(
                SearchKeyword, SelectedCategory, SelectedSourceType, SelectedAlertLevel,
                CurrentPage, PageSize);
            Rules = new ObservableCollection<AlertRule>(result.Items);
            TotalCount = result.Total;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 搜索
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    /// <summary>
    /// 重置筛选条件
    /// </summary>
    [RelayCommand]
    private async Task ResetFilterAsync()
    {
        SearchKeyword = null;
        SelectedCategory = null;
        SelectedSourceType = null;
        SelectedAlertLevel = null;
        CurrentPage = 1;
        await LoadAsync();
    }

    /// <summary>
    /// 上一页
    /// </summary>
    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadAsync();
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadAsync();
        }
    }

    /// <summary>
    /// 添加规则
    /// </summary>
    [RelayCommand]
    private async Task AddAsync()
    {
        var dialog = new AlertRuleEditDialog(_alertRuleRepository, _dbConnectionRepository);

        // 通过DI容器获取主窗体，确保弹窗正确关联
        var mainWindow = App.Services.GetService(typeof(MainWindow)) as System.Windows.Window;
        if (mainWindow != null)
        {
            dialog.Owner = mainWindow;
        }

        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// 编辑规则
    /// </summary>
    [RelayCommand]
    private async Task EditAsync(AlertRule? rule)
    {
        if (rule == null) return;

        var dialog = new AlertRuleEditDialog(_alertRuleRepository, _dbConnectionRepository, rule);

        // 通过DI容器获取主窗体，确保弹窗正确关联
        var mainWindow = App.Services.GetService(typeof(MainWindow)) as System.Windows.Window;
        if (mainWindow != null)
        {
            dialog.Owner = mainWindow;
        }

        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync(AlertRule? rule)
    {
        if (rule == null) return;

        var result = HandyControl.Controls.MessageBox.Show(
            $"确定要删除规则 \"{rule.Name}\" 吗？",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _alertRuleRepository.DeleteAsync(rule.Id);
            Rules.Remove(rule);
        }
    }

    /// <summary>
    /// 切换启用状态
    /// </summary>
    [RelayCommand]
    private async Task ToggleEnabledAsync(AlertRule rule)
    {
        if (rule == null) return;
        rule.Enabled = !rule.Enabled;
        await _alertRuleRepository.UpdateAsync(rule);
    }
}
