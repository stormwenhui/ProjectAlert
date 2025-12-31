using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
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
            var rules = await _alertRuleRepository.GetAllAsync();
            Rules = new ObservableCollection<AlertRule>(rules);
        }
        finally
        {
            IsLoading = false;
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
