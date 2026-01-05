using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Controls;
using ProjectAlert.WPF.Services.TaskQueue;
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
    /// 保存防抖定时器
    /// </summary>
    public DispatcherTimer? SaveDebounceTimer { get; set; }

    /// <summary>
    /// 是否正在保存
    /// </summary>
    public bool IsSaving { get; set; }

    // 事件处理程序引用（用于清理）
    internal DependencyPropertyChangedEventHandler? IsVisibleChangedHandler { get; set; }
    internal EventHandler? LocationChangedHandler { get; set; }
    internal SizeChangedEventHandler? SizeChangedHandler { get; set; }
    internal PropertyChangedEventHandler? PropertyChangedHandler { get; set; }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        // 停止定时器
        if (SaveDebounceTimer != null)
        {
            SaveDebounceTimer.Stop();
            SaveDebounceTimer = null;
        }

        // 取消窗口事件订阅
        if (Window != null)
        {
            if (IsVisibleChangedHandler != null)
                Window.IsVisibleChanged -= IsVisibleChangedHandler;
            if (LocationChangedHandler != null)
                Window.LocationChanged -= LocationChangedHandler;
            if (SizeChangedHandler != null)
                Window.SizeChanged -= SizeChangedHandler;
        }

        // 取消 ViewModel 事件订阅
        if (ViewModel != null && PropertyChangedHandler != null)
        {
            ViewModel.PropertyChanged -= PropertyChangedHandler;
        }

        // 释放 ViewModel
        ViewModel?.Dispose();
        ViewModel = null;

        IsVisibleChangedHandler = null;
        LocationChangedHandler = null;
        SizeChangedHandler = null;
        PropertyChangedHandler = null;
    }
}

/// <summary>
/// 统计浮窗管理服务
/// </summary>
public class StatFloatingWindowService
{
    private readonly IStatConfigRepository _statConfigRepository;
    private readonly IFloatingWindowStateRepository _stateRepository;
    private readonly ITaskQueue _taskQueue;
    private readonly LogService _logService;
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
        IFloatingWindowStateRepository stateRepository,
        ITaskQueue taskQueue,
        LogService logService)
    {
        _statConfigRepository = statConfigRepository;
        _stateRepository = stateRepository;
        _taskQueue = taskQueue;
        _logService = logService;
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
                // 通知 ViewModel 停止任务
                info.ViewModel?.OnWindowHidden();
                // 清理所有资源（事件、定时器、ViewModel）
                info.Cleanup();
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
    /// 恢复所有之前显示的浮窗（错开启动避免并发压力）
    /// </summary>
    public async Task RestoreVisibleWindowsAsync()
    {
        var allStates = await _stateRepository.GetAllAsync();
        var visibleStatWindows = allStates
            .Where(s => s.WindowId.StartsWith("stat_") && s.IsVisible)
            .ToList();

        var random = new Random();
        var delayOffset = 0;

        foreach (var state in visibleStatWindows)
        {
            var configIdStr = state.WindowId.Replace("stat_", "");
            if (int.TryParse(configIdStr, out var configId) && _windows.ContainsKey(configId))
            {
                // 计算延迟时间
                var initialDelay = delayOffset > 0 ? TimeSpan.FromMilliseconds(delayOffset) : (TimeSpan?)null;
                await ShowAsync(configId, initialDelay);

                // 累加延迟（500-2000毫秒）
                delayOffset += random.Next(500, 2001);
            }
        }
    }

    /// <summary>
    /// 显示指定统计的浮窗
    /// </summary>
    /// <param name="configId">配置ID</param>
    /// <param name="initialDelay">初始延迟（用于错开启动）</param>
    public async Task ShowAsync(int configId, TimeSpan? initialDelay = null)
    {
        if (!_windows.TryGetValue(configId, out var info))
            return;

        if (info.Window == null)
        {
            // 加载保存的状态
            var savedState = await _stateRepository.GetByIdAsync(GetWindowId(configId));

            // 创建 ViewModel（通过任务队列刷新数据）
            var viewModel = new StatFloatingViewModel(_taskQueue, _logService);
            info.ViewModel = viewModel;

            // 初始化 ViewModel（只传入配置）
            viewModel.Initialize(info.Config);

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

            // 保存初始延迟，用于可见性变化时
            TimeSpan? capturedDelay = initialDelay;

            // 创建并保存事件处理程序引用（用于后续清理）
            info.IsVisibleChangedHandler = (s, e) =>
            {
                VisibilityChanged?.Invoke(this, (configId, info.Window.IsVisible));

                if (info.Window.IsVisible)
                {
                    // 通知 ViewModel 窗口显示，传入初始延迟
                    info.ViewModel?.OnWindowShown(capturedDelay);
                    // 只在第一次使用延迟，后续显示不需要延迟
                    capturedDelay = null;
                }
                else
                {
                    info.ViewModel?.OnWindowHidden();
                }

                // 保存状态（可见性变化立即保存）
                _ = SaveStateAsync(configId, info);
            };
            info.Window.IsVisibleChanged += info.IsVisibleChangedHandler;

            // 监听窗口位置和大小变化（使用防抖）
            info.LocationChangedHandler = (s, e) => DebounceSaveState(configId, info);
            info.SizeChangedHandler = (s, e) => DebounceSaveState(configId, info);
            info.Window.LocationChanged += info.LocationChangedHandler;
            info.Window.SizeChanged += info.SizeChangedHandler;

            // 监听 ViewModel 属性变化
            info.PropertyChangedHandler = (s, e) =>
            {
                if (e.PropertyName is nameof(viewModel.WindowOpacity)
                    or nameof(viewModel.IsLocked)
                    or nameof(viewModel.IsTopmost))
                {
                    DebounceSaveState(configId, info);
                }
            };
            viewModel.PropertyChanged += info.PropertyChangedHandler;
        }

        info.Window.Show();
        info.Window.Activate();
        // 注：OnWindowShown 会在 IsVisibleChanged 事件中触发，自动请求刷新
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
    /// 显示所有浮窗（错开启动避免并发压力）
    /// </summary>
    public async Task ShowAllAsync()
    {
        var ids = _windows.Keys.ToList();
        var random = new Random();
        var delayOffset = 0;

        foreach (var id in ids)
        {
            // 计算延迟时间
            var initialDelay = delayOffset > 0 ? TimeSpan.FromMilliseconds(delayOffset) : (TimeSpan?)null;
            await ShowAsync(id, initialDelay);

            // 累加延迟（500-2000毫秒）
            delayOffset += random.Next(500, 2001);
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
    /// 防抖保存状态（延迟500ms执行，期间如有新请求则重新计时）
    /// </summary>
    private void DebounceSaveState(int configId, StatFloatingWindowInfo info)
    {
        // 复用同一个 Timer，避免内存泄漏
        if (info.SaveDebounceTimer == null)
        {
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
        }
        else
        {
            info.SaveDebounceTimer.Stop();
        }
        info.SaveDebounceTimer.Start();
    }
}
