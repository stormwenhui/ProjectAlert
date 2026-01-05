using System.Windows;
using System.Windows.Threading;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 悬浮窗管理服务
/// </summary>
public class FloatingWindowService
{
    private const string WindowId = "alert";
    private readonly IFloatingWindowStateRepository _stateRepository;
    private FloatingWidgetWindow? _floatingWindow;
    private FloatingViewModel? _viewModel;
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

    public FloatingWindowService(IFloatingWindowStateRepository stateRepository)
    {
        _stateRepository = stateRepository;
    }

    /// <summary>
    /// 初始化悬浮窗（延迟创建）
    /// </summary>
    public async Task InitializeAsync(FloatingViewModel viewModel)
    {
        _viewModel = viewModel;

        // 加载保存的状态
        var savedState = await _stateRepository.GetByIdAsync(WindowId);

        _floatingWindow = new FloatingWidgetWindow
        {
            DataContext = viewModel,
            WidgetContent = viewModel,
            Width = savedState?.Width ?? 320,
            Height = savedState?.Height ?? 180,
            ShowStatusBar = false,
            CanResize = false
        };

        // 恢复位置
        if (savedState != null)
        {
            _floatingWindow.Left = savedState.Left;
            _floatingWindow.Top = savedState.Top;

            // 恢复 ViewModel 设置
            viewModel.WindowOpacity = savedState.Opacity;
            viewModel.IsLocked = savedState.IsLocked;
            viewModel.IsTopmost = savedState.IsTopmost;
        }
        else
        {
            // 默认位置：屏幕右下角
            var workArea = SystemParameters.WorkArea;
            _floatingWindow.Left = workArea.Right - _floatingWindow.Width - 20;
            _floatingWindow.Top = workArea.Bottom - _floatingWindow.Height - 20;
        }

        // 监听窗口可见性变化
        _floatingWindow.IsVisibleChanged += (s, e) =>
        {
            VisibilityChanged?.Invoke(this, _floatingWindow.IsVisible);

            // 窗口显示时通知 ViewModel 启动任务队列，隐藏时停止
            if (_floatingWindow.IsVisible)
            {
                _viewModel?.OnWindowShown();
            }
            else
            {
                _viewModel?.OnWindowHidden();
            }

            // 保存状态（可见性变化立即保存）
            _ = SaveStateAsync();
        };

        // 监听窗口位置和大小变化（使用防抖）
        _floatingWindow.LocationChanged += (s, e) => DebounceSaveState();
        _floatingWindow.SizeChanged += (s, e) => DebounceSaveState();

        // 监听 ViewModel 属性变化
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(viewModel.WindowOpacity)
                or nameof(viewModel.IsLocked)
                or nameof(viewModel.IsTopmost))
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
    public Task ShowAsync()
    {
        if (_floatingWindow == null || _viewModel == null) return Task.CompletedTask;

        _floatingWindow.Show();
        _floatingWindow.Activate();
        // 注：OnWindowShown 会在 IsVisibleChanged 事件中触发，自动请求刷新
        return Task.CompletedTask;
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
        if (_floatingWindow == null || _viewModel == null) return;

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
    /// 防抖保存状态（延迟500ms执行，期间如有新请求则重新计时）
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
