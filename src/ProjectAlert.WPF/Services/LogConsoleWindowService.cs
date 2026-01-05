using System.Windows;
using System.Windows.Threading;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 日志控制台浮窗管理服务
/// </summary>
public class LogConsoleWindowService
{
    private const string WindowId = "logconsole";
    private readonly IFloatingWindowStateRepository _stateRepository;
    private readonly LogConsoleViewModel _viewModel;
    private FloatingWidgetWindow? _floatingWindow;
    private DispatcherTimer? _saveDebounceTimer;
    private bool _isSaving;

    /// <summary>
    /// 悬浮窗是否可见
    /// </summary>
    public bool IsVisible => _floatingWindow?.IsVisible ?? false;

    /// <summary>
    /// 悬浮窗可见性变化事件
    /// </summary>
    public event EventHandler<bool>? VisibilityChanged;

    public LogConsoleWindowService(
        IFloatingWindowStateRepository stateRepository,
        LogConsoleViewModel viewModel)
    {
        _stateRepository = stateRepository;
        _viewModel = viewModel;
    }

    /// <summary>
    /// 初始化悬浮窗
    /// </summary>
    public async Task InitializeAsync()
    {
        // 加载保存的状态
        var savedState = await _stateRepository.GetByIdAsync(WindowId);

        var content = new LogConsoleContent
        {
            DataContext = _viewModel
        };

        _floatingWindow = new FloatingWidgetWindow
        {
            DataContext = _viewModel,
            WidgetContent = content,
            Width = savedState?.Width ?? 450,
            Height = savedState?.Height ?? 350,
            ShowStatusBar = false,
            CanResize = true
        };

        // 恢复位置
        if (savedState != null)
        {
            _floatingWindow.Left = savedState.Left;
            _floatingWindow.Top = savedState.Top;

            // 恢复 ViewModel 设置
            _viewModel.WindowOpacity = savedState.Opacity;
            _viewModel.IsLocked = savedState.IsLocked;
            _viewModel.IsTopmost = savedState.IsTopmost;
        }
        else
        {
            // 默认位置：屏幕右下角
            var workArea = SystemParameters.WorkArea;
            _floatingWindow.Left = workArea.Right - _floatingWindow.Width - 20;
            _floatingWindow.Top = workArea.Bottom - _floatingWindow.Height - 200;
        }

        // 监听窗口可见性变化
        _floatingWindow.IsVisibleChanged += (s, e) =>
        {
            VisibilityChanged?.Invoke(this, _floatingWindow.IsVisible);
            _ = SaveStateAsync();
        };

        // 监听窗口位置和大小变化（使用防抖）
        _floatingWindow.LocationChanged += (s, e) => DebounceSaveState();
        _floatingWindow.SizeChanged += (s, e) => DebounceSaveState();

        // 监听 ViewModel 属性变化
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(_viewModel.WindowOpacity)
                or nameof(_viewModel.IsLocked)
                or nameof(_viewModel.IsTopmost))
            {
                DebounceSaveState();
            }
        };

        // 如果之前是显示状态，自动显示
        if (savedState?.IsVisible == true)
        {
            await ShowAsync();
        }
    }

    /// <summary>
    /// 显示悬浮窗
    /// </summary>
    public async Task ShowAsync()
    {
        if (_floatingWindow == null) return;

        _floatingWindow.Show();
        _floatingWindow.Activate();
        await _viewModel.RefreshAsync();
    }

    /// <summary>
    /// 隐藏悬浮窗
    /// </summary>
    public void Hide()
    {
        _floatingWindow?.Hide();
    }

    /// <summary>
    /// 切换悬浮窗显示状态
    /// </summary>
    public async Task ToggleAsync()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            await ShowAsync();
        }
    }

    /// <summary>
    /// 保存浮窗状态
    /// </summary>
    private async Task SaveStateAsync()
    {
        if (_floatingWindow == null) return;

        var state = new FloatingWindowState
        {
            WindowId = WindowId,
            IsVisible = _floatingWindow.IsVisible,
            Left = _floatingWindow.Left,
            Top = _floatingWindow.Top,
            Width = _floatingWindow.Width,
            Height = _floatingWindow.Height,
            Opacity = _viewModel.WindowOpacity,
            IsLocked = _viewModel.IsLocked,
            IsTopmost = _viewModel.IsTopmost
        };

        await _stateRepository.SaveAsync(state);
    }

    /// <summary>
    /// 防抖保存状态
    /// </summary>
    private void DebounceSaveState()
    {
        // 复用同一个 Timer，避免内存泄漏
        if (_saveDebounceTimer == null)
        {
            _saveDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _saveDebounceTimer.Tick += async (s, e) =>
            {
                _saveDebounceTimer?.Stop();
                if (!_isSaving)
                {
                    _isSaving = true;
                    try
                    {
                        await SaveStateAsync();
                    }
                    finally
                    {
                        _isSaving = false;
                    }
                }
            };
        }
        else
        {
            _saveDebounceTimer.Stop();
        }
        _saveDebounceTimer.Start();
    }
}
