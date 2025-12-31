using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.WPF.Services;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    /// <summary>
    /// 当前页面
    /// </summary>
    public NavigationService Navigation => _navigationService;

    /// <summary>
    /// 选中的菜单索引
    /// </summary>
    [ObservableProperty]
    private int _selectedMenuIndex;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindowViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    /// <summary>
    /// 导航命令
    /// </summary>
    [RelayCommand]
    private void Navigate(string pageName)
    {
        _navigationService.NavigateTo(pageName);
    }

    /// <summary>
    /// 初始化导航到默认页面
    /// </summary>
    public void Initialize()
    {
        _navigationService.NavigateTo("Dashboard");
    }
}
