using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Views;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 数据库连接视图模型
/// </summary>
public partial class DbConnectionsViewModel : ObservableObject
{
    private readonly IDbConnectionRepository _dbConnectionRepository;

    /// <summary>
    /// 连接列表
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<DbConnection> _connections = new();

    /// <summary>
    /// 选中的连接
    /// </summary>
    [ObservableProperty]
    private DbConnection? _selectedConnection;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private bool _isLoading;

    /// <summary>
    /// 是否没有数据
    /// </summary>
    public bool HasNoData => !IsLoading && Connections.Count == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbConnectionRepository">数据库连接仓储</param>
    public DbConnectionsViewModel(IDbConnectionRepository dbConnectionRepository)
    {
        _dbConnectionRepository = dbConnectionRepository;
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var connections = await _dbConnectionRepository.GetAllAsync();
            Connections = new ObservableCollection<DbConnection>(connections);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 添加连接
    /// </summary>
    [RelayCommand]
    private async Task AddAsync()
    {
        var dialog = new DbConnectionEditDialog(_dbConnectionRepository);

        // 通过DI容器获取主窗体，确保弹窗正确关联
        var mainWindow = App.Services.GetService(typeof(MainWindow)) as System.Windows.Window;
        if (mainWindow != null)
        {
            dialog.Owner = mainWindow;
        }

        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// 编辑连接
    /// </summary>
    [RelayCommand]
    private async Task EditAsync(DbConnection? connection)
    {
        if (connection == null) return;

        var dialog = new DbConnectionEditDialog(_dbConnectionRepository, connection);

        // 通过DI容器获取主窗体，确保弹窗正确关联
        var mainWindow = App.Services.GetService(typeof(MainWindow)) as System.Windows.Window;
        if (mainWindow != null)
        {
            dialog.Owner = mainWindow;
        }

        if (dialog.ShowDialog() == true && dialog.IsSaved)
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// 删除连接
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync(DbConnection? connection)
    {
        if (connection == null) return;

        var result = HandyControl.Controls.MessageBox.Show(
            $"确定要删除连接 \"{connection.Name}\" 吗？",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _dbConnectionRepository.DeleteAsync(connection.Id);
            Connections.Remove(connection);
        }
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    [RelayCommand]
    private async Task TestAsync(DbConnection? connection)
    {
        if (connection == null) return;

        try
        {
            var success = await _dbConnectionRepository.TestConnectionAsync(connection);
            if (success)
            {
                HandyControl.Controls.MessageBox.Show("连接成功！", "测试结果");
            }
            else
            {
                HandyControl.Controls.MessageBox.Show("连接失败", "测试结果");
            }
        }
        catch (Exception ex)
        {
            HandyControl.Controls.MessageBox.Show($"连接失败: {ex.Message}", "测试结果");
        }
    }
}
