using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 已忽略预警视图
/// </summary>
public partial class IgnoredAlertsView : UserControl
{
    private readonly IgnoredAlertsViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public IgnoredAlertsView(IgnoredAlertsViewModel viewModel)
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
