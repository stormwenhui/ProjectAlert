using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 导航服务
/// </summary>
public partial class NavigationService : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _pageTypes = new();

    /// <summary>
    /// 当前页面
    /// </summary>
    [ObservableProperty]
    private UserControl? _currentPage;

    /// <summary>
    /// 当前页面名称
    /// </summary>
    [ObservableProperty]
    private string _currentPageName = string.Empty;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 注册页面
    /// </summary>
    public void RegisterPage<T>(string name) where T : UserControl
    {
        _pageTypes[name] = typeof(T);
    }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    public void NavigateTo(string pageName)
    {
        if (_pageTypes.TryGetValue(pageName, out var pageType))
        {
            var page = _serviceProvider.GetService(pageType) as UserControl;
            if (page != null)
            {
                CurrentPage = page;
                CurrentPageName = pageName;
            }
        }
    }
}
