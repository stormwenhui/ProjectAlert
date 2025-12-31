using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : HandyControl.Controls.Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly IAppSettingRepository _appSettingRepository;
    private bool _isExiting;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow(MainWindowViewModel viewModel, IAppSettingRepository appSettingRepository)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _appSettingRepository = appSettingRepository;
        DataContext = _viewModel;
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _viewModel.Initialize();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_isExiting)
        {
            base.OnClosing(e);
            return;
        }

        // 检查是否设置了最小化到托盘
        var minimizeToTray = await _appSettingRepository.GetByKeyAsync("MinimizeToTray");
        if (minimizeToTray?.Value != "false")
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// 强制退出（用于真正退出应用时调用）
    /// </summary>
    public void ForceClose()
    {
        _isExiting = true;
        Close();
    }

    /// <summary>
    /// 导航按钮选中事件
    /// </summary>
    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is string pageName)
        {
            _viewModel.NavigateCommand.Execute(pageName);
        }
    }
}
