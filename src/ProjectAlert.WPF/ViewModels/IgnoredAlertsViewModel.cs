using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 已忽略预警视图模型
/// </summary>
public partial class IgnoredAlertsViewModel : ObservableObject
{
    private readonly IIgnoredAlertRepository _ignoredAlertRepository;

    /// <summary>
    /// 忽略的预警列表
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<IgnoredAlert> _alerts = new();

    /// <summary>
    /// 选中的预警
    /// </summary>
    [ObservableProperty]
    private IgnoredAlert? _selectedAlert;

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
    public IgnoredAlertsViewModel(IIgnoredAlertRepository ignoredAlertRepository)
    {
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
            var alerts = await _ignoredAlertRepository.GetAllAsync();
            Alerts = new ObservableCollection<IgnoredAlert>(alerts);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 删除忽略记录
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync(IgnoredAlert alert)
    {
        if (alert == null) return;
        await _ignoredAlertRepository.RemoveAsync(alert.Id);
        Alerts.Remove(alert);
    }
}
