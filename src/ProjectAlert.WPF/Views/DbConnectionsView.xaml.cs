using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 数据库连接视图
/// </summary>
public partial class DbConnectionsView : UserControl
{
    private readonly DbConnectionsViewModel _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DbConnectionsView(DbConnectionsViewModel viewModel)
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
