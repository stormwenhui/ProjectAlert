using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ProjectAlert.WPF.Services;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Controls;

/// <summary>
/// 桌面浮窗小组件通用窗口
/// </summary>
public partial class FloatingWidgetWindow : Window
{
    // Win32 API 常量和方法，用于防止窗口被"显示桌面"影响
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private FloatingWidgetViewModelBase? _viewModel;
    private DispatcherTimer? _titleBarHideTimer;
    private DispatcherTimer? _opacityPopupHideTimer;
    private TranslateTransform? _titleBarTransform;
    private bool _isTitleBarVisible = true;
    private FloatingEditModeService? _editModeService;

    /// <summary>
    /// 是否显示状态栏
    /// </summary>
    public bool ShowStatusBar
    {
        get => (bool)GetValue(ShowStatusBarProperty);
        set => SetValue(ShowStatusBarProperty, value);
    }

    public static readonly DependencyProperty ShowStatusBarProperty =
        DependencyProperty.Register(nameof(ShowStatusBar), typeof(bool), typeof(FloatingWidgetWindow), new PropertyMetadata(true));

    /// <summary>
    /// 是否可调整大小
    /// </summary>
    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    public static readonly DependencyProperty CanResizeProperty =
        DependencyProperty.Register(nameof(CanResize), typeof(bool), typeof(FloatingWidgetWindow), new PropertyMetadata(true));

    /// <summary>
    /// 小组件内容
    /// </summary>
    public object? WidgetContent
    {
        get => GetValue(WidgetContentProperty);
        set => SetValue(WidgetContentProperty, value);
    }

    public static readonly DependencyProperty WidgetContentProperty =
        DependencyProperty.Register(nameof(WidgetContent), typeof(object), typeof(FloatingWidgetWindow), new PropertyMetadata(null));

    /// <summary>
    /// 构造函数
    /// </summary>
    public FloatingWidgetWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// 窗口关闭时清理资源
    /// </summary>
    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // 停止并清理定时器
        if (_titleBarHideTimer != null)
        {
            _titleBarHideTimer.Stop();
            _titleBarHideTimer = null;
        }

        if (_opacityPopupHideTimer != null)
        {
            _opacityPopupHideTimer.Stop();
            _opacityPopupHideTimer = null;
        }

        // 取消编辑模式服务事件订阅
        if (_editModeService != null)
        {
            _editModeService.EditModeChanged -= OnEditModeChanged;
            _editModeService = null;
        }

        // 取消事件订阅
        MouseEnter -= OnWindowMouseEnter;
        MouseLeave -= OnWindowMouseLeave;
        DataContextChanged -= OnDataContextChanged;
        Loaded -= OnLoaded;
        Closed -= OnWindowClosed;

        // 取消 ViewModel 事件订阅
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }

    /// <summary>
    /// 窗口源初始化时设置工具窗口样式，防止被"显示桌面"影响
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 获取标题栏TranslateTransform引用
        _titleBarTransform = FindName("TitleBarTransform") as TranslateTransform;

        // 获取编辑模式服务并订阅事件
        _editModeService = App.Services.GetService<FloatingEditModeService>();
        if (_editModeService != null)
        {
            _editModeService.EditModeChanged += OnEditModeChanged;
            // 初始状态应用
            ApplyEditMode(_editModeService.IsEditMode);
        }

        // 透明度按钮悬停事件
        if (FindName("BtnOpacity") is System.Windows.Controls.Button btnOpacity &&
            FindName("OpacityPopup") is System.Windows.Controls.Primitives.Popup opacityPopup &&
            FindName("OpacityPopupBorder") is System.Windows.Controls.Border popupBorder &&
            FindName("OpacityButtonContainer") is System.Windows.Controls.Grid container)
        {
            // 初始化延迟关闭定时器
            _opacityPopupHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _opacityPopupHideTimer.Tick += (s, args) =>
            {
                _opacityPopupHideTimer.Stop();
                opacityPopup.IsOpen = false;
            };

            // 鼠标进入按钮或Popup时显示
            btnOpacity.MouseEnter += (s, args) =>
            {
                _opacityPopupHideTimer?.Stop();
                opacityPopup.IsOpen = true;
            };
            popupBorder.MouseEnter += (s, args) => _opacityPopupHideTimer?.Stop();

            // 鼠标离开按钮或Popup时延迟关闭
            container.MouseLeave += (s, args) => _opacityPopupHideTimer?.Start();
            popupBorder.MouseLeave += (s, args) => _opacityPopupHideTimer?.Start();
        }

        // 鼠标进入/离开事件，用于控制标题栏显示
        MouseEnter += OnWindowMouseEnter;
        MouseLeave += OnWindowMouseLeave;

        // 启动3秒定时器，之后隐藏标题栏
        _titleBarHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _titleBarHideTimer.Tick += (s, args) =>
        {
            _titleBarHideTimer.Stop();
            HideTitleBar();
        };
        _titleBarHideTimer.Start();
    }

    /// <summary>
    /// 编辑模式变化事件处理
    /// </summary>
    private void OnEditModeChanged(object? sender, bool isEditMode)
    {
        Dispatcher.Invoke(() => ApplyEditMode(isEditMode));
    }

    /// <summary>
    /// 应用编辑模式状态
    /// </summary>
    private void ApplyEditMode(bool isEditMode)
    {
        if (isEditMode)
        {
            // 编辑模式：启用交互，显示标题栏
            ShowTitleBar();
            _titleBarHideTimer?.Stop();
            // 根据锁定状态更新 ResizeMode
            if (_viewModel != null)
            {
                UpdateResizeMode(_viewModel.IsLocked);
            }
        }
        else
        {
            // 静态模式：隐藏标题栏，禁用交互
            HideTitleBar();
            _titleBarHideTimer?.Stop();
            // 禁用调整大小
            ResizeMode = ResizeMode.NoResize;
        }
    }

    private void OnWindowMouseEnter(object sender, MouseEventArgs e)
    {
        // 非编辑模式下不响应鼠标进入
        if (_editModeService?.IsEditMode != true) return;

        _titleBarHideTimer?.Stop();
        ShowTitleBar();
    }

    private void OnWindowMouseLeave(object sender, MouseEventArgs e)
    {
        // 非编辑模式下不响应鼠标离开
        if (_editModeService?.IsEditMode != true) return;

        // 鼠标离开后延迟隐藏标题栏
        _titleBarHideTimer?.Start();
    }

    private void ShowTitleBar()
    {
        if (_isTitleBarVisible || _titleBarTransform == null) return;
        _isTitleBarVisible = true;

        var animation = new DoubleAnimation(-36, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _titleBarTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    private void HideTitleBar()
    {
        if (!_isTitleBarVisible || _titleBarTransform == null) return;
        _isTitleBarVisible = false;

        var animation = new DoubleAnimation(0, -36, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        _titleBarTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // 取消订阅旧 ViewModel 的属性变化事件
        if (e.OldValue is FloatingWidgetViewModelBase oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as FloatingWidgetViewModelBase;

        // 订阅新 ViewModel 的属性变化事件
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            // 初始化时根据锁定状态设置 ResizeMode
            UpdateResizeMode(_viewModel.IsLocked);
        }
    }

    /// <summary>
    /// ViewModel 属性变化事件处理
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FloatingWidgetViewModelBase.IsLocked) && _viewModel != null)
        {
            UpdateResizeMode(_viewModel.IsLocked);
        }
    }

    /// <summary>
    /// 根据锁定状态更新窗口的 ResizeMode
    /// </summary>
    private void UpdateResizeMode(bool isLocked)
    {
        // 非编辑模式下始终禁用调整大小
        if (_editModeService?.IsEditMode != true)
        {
            ResizeMode = ResizeMode.NoResize;
            return;
        }

        ResizeMode = isLocked ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;
    }

    /// <summary>
    /// 窗口鼠标左键按下事件，用于拖动窗口
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 非编辑模式下不允许拖动
        if (_editModeService?.IsEditMode != true) return;

        // 如果已锁定，不允许拖动
        if (_viewModel?.IsLocked == true) return;
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    /// <summary>
    /// 锁定按钮点击事件
    /// </summary>
    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsLocked = !_viewModel.IsLocked;
        }
    }

    /// <summary>
    /// 置顶按钮点击事件
    /// </summary>
    private void TopmostButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsTopmost = !_viewModel.IsTopmost;
        }
    }

    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    /// <summary>
    /// 最小化按钮点击事件
    /// </summary>
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    /// <summary>
    /// 关闭按钮点击事件（只隐藏，不退出程序）
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    public async Task RefreshAsync()
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    /// <summary>
    /// 外层Border大小改变时更新圆角裁剪区域
    /// </summary>
    private void OuterBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border)
        {
            border.Clip = new RectangleGeometry(
                new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                12, 12);
        }
    }
}
