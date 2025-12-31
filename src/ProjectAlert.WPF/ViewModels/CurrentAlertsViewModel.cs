using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 当前预警视图模型
/// </summary>
public partial class CurrentAlertsViewModel : ObservableObject
{
    private readonly ICurrentAlertRepository _currentAlertRepository;
    private readonly IIgnoredAlertRepository _ignoredAlertRepository;

    /// <summary>
    /// 预警列表
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<CurrentAlert> _alerts = new();

    /// <summary>
    /// 选中的预警
    /// </summary>
    [ObservableProperty]
    private CurrentAlert? _selectedAlert;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private bool _isLoading;

    /// <summary>
    /// 是否没有数据
    /// </summary>
    public bool HasNoData => !IsLoading && Alerts.Count == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CurrentAlertsViewModel(
        ICurrentAlertRepository currentAlertRepository,
        IIgnoredAlertRepository ignoredAlertRepository)
    {
        _currentAlertRepository = currentAlertRepository;
        _ignoredAlertRepository = ignoredAlertRepository;
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
            var alerts = await _currentAlertRepository.GetAllAsync();
            Alerts = new ObservableCollection<CurrentAlert>(alerts);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 忽略预警
    /// </summary>
    [RelayCommand]
    private async Task IgnoreAsync(CurrentAlert alert)
    {
        if (alert == null) return;

        var ignored = new IgnoredAlert
        {
            RuleId = alert.RuleId,
            AlertKey = alert.AlertKey,
            IgnoredAt = DateTime.Now,
            IgnoredReason = "手动忽略"
        };

        await _ignoredAlertRepository.AddAsync(ignored);
        await _currentAlertRepository.DeleteAsync(alert.Id);
        Alerts.Remove(alert);
    }
}
