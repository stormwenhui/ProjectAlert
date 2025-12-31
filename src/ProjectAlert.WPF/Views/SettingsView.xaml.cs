using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 设置视图
/// </summary>
public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
