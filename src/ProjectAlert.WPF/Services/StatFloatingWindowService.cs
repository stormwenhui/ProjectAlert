using System.Windows;
using System.Windows.Threading;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.Repository;
using ProjectAlert.WPF.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 统计浮窗项信息
/// </summary>
public class StatFloatingWindowInfo
{
    /// <summary>
    /// 统计配置
    /// </summary>
    public StatConfig Config { get; set; } = null!;

    /// <summary>
    /// 浮窗实例
    /// </summary>
    public FloatingWidgetWindow? Window { get; set; }

    /// <summary>
    /// 视图模型
    /// </summary>
    public StatFloatingViewModel? ViewModel { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible => Window?.IsVisible ?? false;

    /// <summary>
    /// 刷新定时器
    /// </summary>
    public DispatcherTimer? RefreshTimer { get; set; }

    /// <summary>
    /// 保存防抖定时器
    /// </summary>
    public DispatcherTimer? SaveDebounceTimer { get; set; }

    /// <summary>
    /// 是否正在保存
    /// </summary>
    public bool IsSaving { get; set; }
}

/// <summary>
/// 统计浮窗管理服务
/// </summary>
public class StatFloatingWindowService
{
    private readonly IStatConfigRepository _statConfigRepository;
    private readonly IDbConnectionRepository _dbConnectionRepository;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IFloatingWindowStateRepository _stateRepository;
    private readonly Dictionary<int, StatFloatingWindowInfo> _windows = new();

    /// <summary>
    /// 浮窗列表变化事件
    /// </summary>
    public event EventHandler? WindowsChanged;

    /// <summary>
    /// 浮窗可见性变化事件
    /// </summary>
    public event EventHandler<(int ConfigId, bool IsVisible)>? VisibilityChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StatFloatingWindowService(
        IStatConfigRepository statConfigRepository,
        IDbConnectionRepository dbConnectionRepository,
        IDbConnectionFactory dbConnectionFactory,
        IFloatingWindowStateRepository stateRepository)
    {
        _statConfigRepository = statConfigRepository;
        _dbConnectionRepository = dbConnectionRepository;
        _dbConnectionFactory = dbConnectionFactory;
        _stateRepository = stateRepository;
    }

    /// <summary>
    /// 获取浮窗状态存储ID
    /// </summary>
    private static string GetWindowId(int configId) => $"stat_{configId}";

    /// <summary>
    /// 获取所有浮窗信息
    /// </summary>
    public IReadOnlyList<StatFloatingWindowInfo> GetAllWindows()
    {
        return _windows.Values.ToList();
    }

    /// <summary>
    /// 刷新统计配置列表
    /// </summary>
    public async Task RefreshConfigsAsync()
    {
        var configs = await _statConfigRepository.GetEnabledAsync();
        var configList = configs.ToList();

        // 移除不存在的配置对应的浮窗
        var idsToRemove = _windows.Keys.Where(id => !configList.Any(c => c.Id == id)).ToList();
        foreach (var id in idsToRemove)
        {
            if (_windows.TryGetValue(id, out var info))
            {
                info.RefreshTimer?.Stop();
                info.Window?.Close();
                _windows.Remove(id);
                // 删除保存的状态
                await _stateRepository.DeleteAsync(GetWindowId(id));
            }
        }

        // 添加新的配置
        foreach (var config in configList)
        {
            if (!_windows.ContainsKey(config.Id))
            {
                _windows[config.Id] = new StatFloatingWindowInfo { Config = config };
            }
            else
            {
                // 更新配置
                _windows[config.Id].Config = config;
            }
        }

        WindowsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 恢复所有之前显示的浮窗
    /// </summary>
    public async Task RestoreVisibleWindowsAsync()
    {
        var allStates = await _stateRepository.GetAllAsync();
        var visibleStatWindows = allStates
            .Where(s => s.WindowId.StartsWith("stat_") && s.IsVisible)
            .ToList();

        foreach (var state in visibleStatWindows)
        {
            var configIdStr = state.WindowId.Replace("stat_", "");
            if (int.TryParse(configIdStr, out var configId) && _windows.ContainsKey(configId))
            {
                await ShowAsync(configId);
            }
        }
    }

    /// <summary>
    /// 显示指定统计的浮窗
    /// </summary>
    public async Task ShowAsync(int configId)
    {
        if (!_windows.TryGetValue(configId, out var info))
            return;

        if (info.Window == null)
        {
            // 加载保存的状态
            var savedState = await _stateRepository.GetByIdAsync(GetWindowId(configId));

            // 创建 ViewModel
            var viewModel = new StatFloatingViewModel(_dbConnectionFactory);
            info.ViewModel = viewModel;

            // 获取数据库连接
            DbConnection? dbConnection = null;
            if (info.Config.DbConnectionId.HasValue)
            {
                dbConnection = await _dbConnectionRepository.GetByIdAsync(info.Config.DbConnectionId.Value);
            }

            // 初始化 ViewModel
            viewModel.Initialize(info.Config, dbConnection);

            // 恢复 ViewModel 设置
            if (savedState != null)
            {
                viewModel.WindowOpacity = savedState.Opacity;
                viewModel.IsLocked = savedState.IsLocked;
                viewModel.IsTopmost = savedState.IsTopmost;
            }

            // 创建通用浮窗
            info.Window = new FloatingWidgetWindow
            {
                DataContext = viewModel,
                WidgetContent = viewModel,
                Width = savedState?.Width ?? 400,
                Height = savedState?.Height ?? 300,
                ShowStatusBar = true,
                CanResize = true
            };

            // 恢复位置
            if (savedState != null)
            {
                info.Window.Left = savedState.Left;
                info.Window.Top = savedState.Top;
            }
            else
            {
                // 默认位置：错开排列
                var workArea = SystemParameters.WorkArea;
                var index = _windows.Values.ToList().FindIndex(w => w.Config.Id == configId);
                info.Window.Left = workArea.Right - info.Window.Width - 20 - (index % 3) * 50;
                info.Window.Top = workArea.Bottom - info.Window.Height - 20 - (index / 3) * 50;
            }

            // 监听窗口可见性变化
            info.Window.IsVisibleChanged += (s, e) =>
            {
                VisibilityChanged?.Invoke(this, (configId, info.Window.IsVisible));

                if (info.Window.IsVisible)
                {
                    StartRefreshTimer(info);
                }
                else
                {
                    StopRefreshTimer(info);
                }

                // 保存状态（可见性变化立即保存）
                _ = SaveStateAsync(configId, info);
            };

            // 监听窗口位置和大小变化（使用防抖）
            info.Window.LocationChanged += (s, e) => DebounceSaveState(configId, info);
            info.Window.SizeChanged += (s, e) => DebounceSaveState(configId, info);

            // 监听 ViewModel 属性变化
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(viewModel.WindowOpacity)
                    or nameof(viewModel.IsLocked)
                    or nameof(viewModel.IsTopmost))
                {
                    DebounceSaveState(configId, info);
                }
            };
        }

        info.Window.Show();
        info.Window.Activate();
        if (info.ViewModel != null)
        {
            await info.ViewModel.RefreshAsync();
        }
    }

    /// <summary>
    /// 隐藏指定统计的浮窗
    /// </summary>
    public void Hide(int configId)
    {
        if (_windows.TryGetValue(configId, out var info))
        {
            info.Window?.Hide();
        }
    }

    /// <summary>
    /// 切换指定统计浮窗的显示状态
    /// </summary>
    public async Task ToggleAsync(int configId)
    {
        if (!_windows.TryGetValue(configId, out var info))
            return;

        if (info.IsVisible)
        {
            Hide(configId);
        }
        else
        {
            await ShowAsync(configId);
        }
    }

    /// <summary>
    /// 显示所有浮窗
    /// </summary>
    public async Task ShowAllAsync()
    {
        foreach (var id in _windows.Keys.ToList())
        {
            await ShowAsync(id);
        }
    }

    /// <summary>
    /// 隐藏所有浮窗
    /// </summary>
    public void HideAll()
    {
        foreach (var id in _windows.Keys.ToList())
        {
            Hide(id);
        }
    }

    /// <summary>
    /// 检查指定统计浮窗是否可见
    /// </summary>
    public bool IsVisible(int configId)
    {
        return _windows.TryGetValue(configId, out var info) && info.IsVisible;
    }

    /// <summary>
    /// 保存浮窗状态
    /// </summary>
    private async Task SaveStateAsync(int configId, StatFloatingWindowInfo info)
    {
        if (info.Window == null || info.ViewModel == null) return;

        var state = new FloatingWindowState
        {
            WindowId = GetWindowId(configId),
            IsVisible = info.Window.IsVisible,
            Left = info.Window.Left,
            Top = info.Window.Top,
            Width = info.Window.Width,
            Height = info.Window.Height,
            Opacity = info.ViewModel.WindowOpacity,
            IsLocked = info.ViewModel.IsLocked,
            IsTopmost = info.ViewModel.IsTopmost
        };

        await _stateRepository.SaveAsync(state);
    }

    /// <summary>
    /// 启动定时刷新
    /// </summary>
    private void StartRefreshTimer(StatFloatingWindowInfo info)
    {
        if (info.RefreshTimer != null) return;

        var interval = info.Config.RefreshInterval > 0 ? info.Config.RefreshInterval : 60;

        info.RefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(interval)
        };
        info.RefreshTimer.Tick += async (s, e) =>
        {
            if (info.ViewModel != null)
            {
                await info.ViewModel.RefreshAsync();
            }
        };
        info.RefreshTimer.Start();
    }

    /// <summary>
    /// 停止定时刷新
    /// </summary>
    private void StopRefreshTimer(StatFloatingWindowInfo info)
    {
        info.RefreshTimer?.Stop();
        info.RefreshTimer = null;
    }

    /// <summary>
    /// 防抖保存状态（延迟500ms执行，期间如有新请求则重新计时）
    /// </summary>
    private void DebounceSaveState(int configId, StatFloatingWindowInfo info)
    {
        info.SaveDebounceTimer?.Stop();
        info.SaveDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        info.SaveDebounceTimer.Tick += async (s, e) =>
        {
            info.SaveDebounceTimer?.Stop();
            if (!info.IsSaving)
            {
                info.IsSaving = true;
                try
                {
                    await SaveStateAsync(configId, info);
                }
                finally
                {
                    info.IsSaving = false;
                }
            }
        };
        info.SaveDebounceTimer.Start();
    }
}
