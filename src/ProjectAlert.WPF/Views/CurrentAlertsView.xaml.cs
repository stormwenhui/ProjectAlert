using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 当前预警视图
/// </summary>
public partial class CurrentAlertsView : UserControl
{
    private readonly CurrentAlertsViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CurrentAlertsView(CurrentAlertsViewModel viewModel)
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
