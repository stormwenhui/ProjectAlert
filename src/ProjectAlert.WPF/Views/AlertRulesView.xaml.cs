using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 预警规则视图
/// </summary>
public partial class AlertRulesView : UserControl
{
    private readonly AlertRulesViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AlertRulesView(AlertRulesViewModel viewModel)
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
