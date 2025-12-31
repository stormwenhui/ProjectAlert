using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 设置视图模型
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingRepository _appSettingRepository;

    /// <summary>
    /// 是否开机启动
    /// </summary>
    [ObservableProperty]
    private bool _autoStart;

    /// <summary>
    /// 是否显示悬浮窗
    /// </summary>
    [ObservableProperty]
    private bool _showFloatingWindow = true;

    /// <summary>
    /// 是否最小化到托盘
    /// </summary>
    [ObservableProperty]
    private bool _minimizeToTray = true;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SettingsViewModel(IAppSettingRepository appSettingRepository)
    {
        _appSettingRepository = appSettingRepository;
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        var autoStart = await _appSettingRepository.GetByKeyAsync("AutoStart");
        AutoStart = autoStart?.Value == "true";

        var showFloating = await _appSettingRepository.GetByKeyAsync("ShowFloatingWindow");
        ShowFloatingWindow = showFloating?.Value != "false";

        var minimizeToTray = await _appSettingRepository.GetByKeyAsync("MinimizeToTray");
        MinimizeToTray = minimizeToTray?.Value != "false";
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        await _appSettingRepository.SetValueAsync("AutoStart", AutoStart.ToString().ToLower());
        await _appSettingRepository.SetValueAsync("ShowFloatingWindow", ShowFloatingWindow.ToString().ToLower());
        await _appSettingRepository.SetValueAsync("MinimizeToTray", MinimizeToTray.ToString().ToLower());
    }
}
