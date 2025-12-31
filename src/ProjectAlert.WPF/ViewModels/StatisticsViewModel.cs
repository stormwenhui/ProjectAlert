using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Views;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 统计视图模型
/// </summary>
public partial class StatisticsViewModel : ObservableObject
{
    private readonly IStatConfigRepository _statConfigRepository;
    private readonly IDbConnectionRepository _dbConnectionRepository;

    /// <summary>
    /// 统计配置列表
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<StatConfig> _configs = new();

    /// <summary>
    /// 选中的配置
    /// </summary>
    [ObservableProperty]
    private StatConfig? _selectedConfig;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private bool _isLoading;

    /// <summary>
    /// 是否没有数据
    /// </summary>
    public bool HasNoData => !IsLoading && Configs.Count == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="statConfigRepository">统计配置仓储</param>
    /// <param name="dbConnectionRepository">数据库连接仓储</param>
    public StatisticsViewModel(
        IStatConfigRepository statConfigRepository,
        IDbConnectionRepository dbConnectionRepository)
    {
        _statConfigRepository = statConfigRepository;
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
            var configs = await _statConfigRepository.GetAllAsync();
            Configs = new ObservableCollection<StatConfig>(configs);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 添加配置
    /// </summary>
    [RelayCommand]
    private async Task AddAsync()
    {
        var dialog = new StatConfigEditDialog(_statConfigRepository, _dbConnectionRepository);

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
    /// 编辑配置
    /// </summary>
    [RelayCommand]
    private async Task EditAsync(StatConfig? config)
    {
        if (config == null) return;

        var dialog = new StatConfigEditDialog(_statConfigRepository, _dbConnectionRepository, config);

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
    /// 删除配置
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync(StatConfig? config)
    {
        if (config == null) return;

        var result = HandyControl.Controls.MessageBox.Show(
            $"确定要删除统计配置 \"{config.Name}\" 吗？",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _statConfigRepository.DeleteAsync(config.Id);
            Configs.Remove(config);
        }
    }
}
