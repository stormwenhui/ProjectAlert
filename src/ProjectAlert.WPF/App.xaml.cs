using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.Repository;
using ProjectAlert.Repository.Repositories;
using ProjectAlert.WPF.Services;
using ProjectAlert.WPF.Services.Executor;
using ProjectAlert.WPF.Services.Scheduler;
using ProjectAlert.WPF.ViewModels;
using ProjectAlert.WPF.Views;
using Quartz;

namespace ProjectAlert.WPF;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private TrayIconService? _trayIconService;

    /// <summary>
    /// 服务提供者
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 应用程序启动
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        Services = _host.Services;
        await _host.StartAsync();

        // 注册导航页面
        var navigationService = Services.GetRequiredService<NavigationService>();
        navigationService.RegisterPage<DashboardView>("Dashboard");
        navigationService.RegisterPage<CurrentAlertsView>("CurrentAlerts");
        navigationService.RegisterPage<IgnoredAlertsView>("IgnoredAlerts");
        navigationService.RegisterPage<AlertRulesView>("AlertRules");
        navigationService.RegisterPage<DbConnectionsView>("DbConnections");
        navigationService.RegisterPage<StatisticsView>("Statistics");
        navigationService.RegisterPage<SettingsView>("Settings");

        // 初始化托盘图标
        _trayIconService = Services.GetRequiredService<TrayIconService>();
        _trayIconService.Initialize();
        _trayIconService.ShowMainWindowRequested += OnShowMainWindow;
        _trayIconService.ExitRequested += OnExitRequested;
        _trayIconService.ToggleFloatingWindowRequested += OnToggleFloatingWindow;

        // 初始化悬浮窗服务（自动恢复上次状态）
        var floatingWindowService = Services.GetRequiredService<FloatingWindowService>();
        var floatingViewModel = Services.GetRequiredService<FloatingViewModel>();
        await floatingWindowService.InitializeAsync(floatingViewModel);

        // 初始化统计浮窗服务（自动恢复上次状态）
        var statFloatingWindowService = Services.GetRequiredService<StatFloatingWindowService>();
        await statFloatingWindowService.RefreshConfigsAsync();
        await statFloatingWindowService.RestoreVisibleWindowsAsync();

        // 初始化日志控制台浮窗服务
        var logConsoleWindowService = Services.GetRequiredService<LogConsoleWindowService>();
        await logConsoleWindowService.InitializeAsync();

        // 记录启动日志
        var logService = Services.GetRequiredService<LogService>();
        logService.Info("系统", "项目监控系统已启动");

        // 启动调度服务
        var schedulerService = Services.GetRequiredService<SchedulerService>();
        await schedulerService.StartAsync();

        // 显示主窗口并显式设置为Application的MainWindow
        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;  // 显式设置，确保弹窗Owner正确
        mainWindow.Show();

        // 默认导航到控制面板
        navigationService.NavigateTo("Dashboard");
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void OnShowMainWindow()
    {
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        mainWindow.Activate();
        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }
    }

    /// <summary>
    /// 切换悬浮窗显示状态
    /// </summary>
    private async void OnToggleFloatingWindow()
    {
        var floatingWindowService = Services.GetRequiredService<FloatingWindowService>();
        await floatingWindowService.ToggleAsync();
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    private void OnExitRequested()
    {
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.ForceClose();
        Shutdown();
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // SQLite 数据库上下文
        services.AddSingleton<SqliteContext>();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // 仓储
        services.AddScoped<IDbConnectionRepository, DbConnectionRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<ICurrentAlertRepository, CurrentAlertRepository>();
        services.AddScoped<IIgnoredAlertRepository, IgnoredAlertRepository>();
        services.AddScoped<IStatConfigRepository, StatConfigRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddSingleton<IFloatingWindowStateRepository, FloatingWindowStateRepository>();

        // Quartz 调度器
        services.AddQuartz();

        // HTTP 客户端
        services.AddHttpClient();

        // 调度服务
        services.AddSingleton<SchedulerService>();
        services.AddTransient<AlertCheckJob>();

        // 预警执行器
        services.AddScoped<SqlAlertExecutor>();
        services.AddScoped<ApiAlertExecutor>();

        // 导航服务
        services.AddSingleton<NavigationService>();

        // 悬浮窗服务
        services.AddSingleton<FloatingWindowService>();
        services.AddSingleton<StatFloatingWindowService>();
        services.AddSingleton<LogConsoleWindowService>();

        // 日志服务
        services.AddSingleton<LogService>();

        // 托盘图标服务
        services.AddSingleton<TrayIconService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<FloatingViewModel>();
        services.AddTransient<StatFloatingViewModel>();
        services.AddSingleton<LogConsoleViewModel>();
        services.AddTransient<CurrentAlertsViewModel>();
        services.AddTransient<IgnoredAlertsViewModel>();
        services.AddTransient<AlertRulesViewModel>();
        services.AddTransient<DbConnectionsViewModel>();
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        // Views
        services.AddTransient<DashboardView>();
        services.AddTransient<CurrentAlertsView>();
        services.AddTransient<IgnoredAlertsView>();
        services.AddTransient<AlertRulesView>();
        services.AddTransient<DbConnectionsView>();
        services.AddTransient<StatisticsView>();
        services.AddTransient<SettingsView>();
    }

    /// <summary>
    /// 应用程序退出
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        // 清理托盘图标
        _trayIconService?.Dispose();

        // 停止调度服务
        var schedulerService = Services.GetService<SchedulerService>();
        if (schedulerService != null)
        {
            await schedulerService.StopAsync();
        }

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
