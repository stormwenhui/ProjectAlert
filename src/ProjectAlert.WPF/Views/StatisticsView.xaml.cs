using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 统计视图
/// </summary>
public partial class StatisticsView : UserControl
{
    private readonly StatisticsViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StatisticsView(StatisticsViewModel viewModel)
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
