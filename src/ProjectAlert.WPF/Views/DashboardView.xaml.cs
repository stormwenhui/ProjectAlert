using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProjectAlert.WPF.Services;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 浮窗按钮信息
/// </summary>
internal class FloatingButtonInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public Func<Task> ToggleAction { get; set; } = null!;
    public Button? Button { get; set; }
}

/// <summary>
/// 控制面板视图
/// </summary>
public partial class DashboardView : UserControl
{
    private readonly FloatingWindowService _floatingWindowService;
    private readonly LogConsoleWindowService _logConsoleWindowService;
    private readonly StatFloatingWindowService _statFloatingWindowService;
    private readonly NavigationService _navigationService;
    private readonly FloatingEditModeService _editModeService;

    private readonly DispatcherTimer _resourceTimer;
    private readonly Process _currentProcess;
    private DateTime _lastCpuCheckTime;
    private TimeSpan _lastCpuTime;

    // 浮窗按钮集合
    private readonly List<FloatingButtonInfo> _floatingButtons = new();

    // 防止重入标志
    private bool _isRefreshing;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DashboardView()
    {
        InitializeComponent();

        _floatingWindowService = App.Services.GetRequiredService<FloatingWindowService>();
        _logConsoleWindowService = App.Services.GetRequiredService<LogConsoleWindowService>();
        _statFloatingWindowService = App.Services.GetRequiredService<StatFloatingWindowService>();
        _navigationService = App.Services.GetRequiredService<NavigationService>();
        _editModeService = App.Services.GetRequiredService<FloatingEditModeService>();

        // 订阅悬浮窗可见性变化事件
        _floatingWindowService.VisibilityChanged += OnAnyFloatingVisibilityChanged;
        _logConsoleWindowService.VisibilityChanged += OnAnyFloatingVisibilityChanged;

        // 订阅统计浮窗事件
        _statFloatingWindowService.WindowsChanged += OnStatFloatingWindowsChanged;
        _statFloatingWindowService.VisibilityChanged += OnStatFloatingVisibilityChanged;

        // 订阅编辑模式变化事件
        _editModeService.EditModeChanged += OnEditModeChanged;

        // 初始化进程监控
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuCheckTime = DateTime.UtcNow;
        _lastCpuTime = _currentProcess.TotalProcessorTime;

        // 初始化资源刷新定时器（每5秒刷新）
        _resourceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _resourceTimer.Tick += OnResourceTimerTick;

        // 注册 Unloaded 事件
        Unloaded += UserControl_Unloaded;
    }

    /// <summary>
    /// 资源定时器回调
    /// </summary>
    private void OnResourceTimerTick(object? sender, EventArgs e)
    {
        RefreshResourceStats();
    }

    /// <summary>
    /// 页面卸载事件 - 清理资源
    /// </summary>
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        // 停止定时器
        _resourceTimer.Stop();
        _resourceTimer.Tick -= OnResourceTimerTick;

        // 取消事件订阅
        _floatingWindowService.VisibilityChanged -= OnAnyFloatingVisibilityChanged;
        _logConsoleWindowService.VisibilityChanged -= OnAnyFloatingVisibilityChanged;
        _statFloatingWindowService.WindowsChanged -= OnStatFloatingWindowsChanged;
        _statFloatingWindowService.VisibilityChanged -= OnStatFloatingVisibilityChanged;
        _editModeService.EditModeChanged -= OnEditModeChanged;

        Unloaded -= UserControl_Unloaded;

        // 清理按钮事件
        foreach (var info in _floatingButtons)
        {
            if (info.Button != null)
            {
                info.Button.Click -= OnFloatingButtonClick;
            }
        }
        _floatingButtons.Clear();
    }

    /// <summary>
    /// 页面加载事件
    /// </summary>
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshFloatingButtonsAsync();

        // 初始刷新资源统计并启动定时器
        RefreshResourceStats();
        _resourceTimer.Start();

        // 初始化编辑模式按钮状态
        UpdateEditModeButton(_editModeService.IsEditMode);

        // 注册卡片点击事件
        try
        {
            var borderThread = FindName("BorderThreadCount") as Border;
            var borderHandle = FindName("BorderHandleCount") as Border;

            if (borderThread != null)
            {
                borderThread.MouseLeftButtonDown += ThreadCount_Click;
                Debug.WriteLine("已注册线程数卡片点击事件");
            }
            else
            {
                Debug.WriteLine("错误: 找不到BorderThreadCount元素");
            }

            if (borderHandle != null)
            {
                borderHandle.MouseLeftButtonDown += HandleCount_Click;
                Debug.WriteLine("已注册句柄数卡片点击事件");
            }
            else
            {
                Debug.WriteLine("错误: 找不到BorderHandleCount元素");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"注册点击事件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 任意浮窗可见性变化事件处理
    /// </summary>
    private void OnAnyFloatingVisibilityChanged(object? sender, bool isVisible)
    {
        Dispatcher.Invoke(RefreshFloatingButtonsUI);
    }

    /// <summary>
    /// 统计浮窗列表变化事件处理
    /// </summary>
    private void OnStatFloatingWindowsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => _ = RefreshFloatingButtonsAsync());
    }

    /// <summary>
    /// 统计浮窗可见性变化事件处理
    /// </summary>
    private void OnStatFloatingVisibilityChanged(object? sender, (int ConfigId, bool IsVisible) e)
    {
        Dispatcher.Invoke(RefreshFloatingButtonsUI);
    }

    /// <summary>
    /// 刷新浮窗按钮列表
    /// </summary>
    private async Task RefreshFloatingButtonsAsync()
    {
        // 防止重入（RefreshConfigsAsync 会触发 WindowsChanged 事件）
        if (_isRefreshing) return;

        _isRefreshing = true;
        try
        {
            await _statFloatingWindowService.RefreshConfigsAsync();
            RebuildFloatingButtons();
            RefreshFloatingButtonsUI();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// 重建浮窗按钮集合
    /// </summary>
    private void RebuildFloatingButtons()
    {
        _floatingButtons.Clear();
        FloatingButtonPanel.Children.Clear();

        // 1. 预警浮窗
        var alertBtn = new FloatingButtonInfo
        {
            Id = "alert",
            Name = "预警浮窗",
            Category = "系统",
            IsVisible = _floatingWindowService.IsVisible,
            ToggleAction = _floatingWindowService.ToggleAsync
        };
        _floatingButtons.Add(alertBtn);
        alertBtn.Button = CreateFloatingButton(alertBtn);
        FloatingButtonPanel.Children.Add(alertBtn.Button);

        // 2. 日志浮窗
        var logBtn = new FloatingButtonInfo
        {
            Id = "logconsole",
            Name = "日志浮窗",
            Category = "系统",
            IsVisible = _logConsoleWindowService.IsVisible,
            ToggleAction = _logConsoleWindowService.ToggleAsync
        };
        _floatingButtons.Add(logBtn);
        logBtn.Button = CreateFloatingButton(logBtn);
        FloatingButtonPanel.Children.Add(logBtn.Button);

        // 3. 统计浮窗（循环每一个）
        var statWindows = _statFloatingWindowService.GetAllWindows();
        foreach (var info in statWindows)
        {
            var configId = info.Config.Id;
            var statBtn = new FloatingButtonInfo
            {
                Id = $"stat_{configId}",
                Name = info.Config.Name,
                Category = info.Config.Category.ToString(),
                IsVisible = info.IsVisible,
                ToggleAction = () => _statFloatingWindowService.ToggleAsync(configId)
            };
            _floatingButtons.Add(statBtn);
            statBtn.Button = CreateFloatingButton(statBtn);
            FloatingButtonPanel.Children.Add(statBtn.Button);
        }

        TxtNoFloatingWindows.Visibility = _floatingButtons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 创建浮窗控制按钮
    /// </summary>
    private Button CreateFloatingButton(FloatingButtonInfo info)
    {
        var button = new Button
        {
            Tag = info,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(16, 8, 16, 8),
            FontSize = 13,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        button.Click += OnFloatingButtonClick;
        UpdateButtonAppearance(button, info);

        return button;
    }

    /// <summary>
    /// 更新按钮外观
    /// </summary>
    private void UpdateButtonAppearance(Button button, FloatingButtonInfo info)
    {
        var statusText = info.IsVisible ? "开" : "关";
        button.Content = $"{info.Name}（{statusText}）";

        // 根据状态设置样式
        if (info.IsVisible)
        {
            button.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // #E8F5E9
            button.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));   // #4CAF50
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }
        else
        {
            button.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #F5F5F5
            button.Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97));    // #616161
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(189, 189, 189)); // #BDBDBD
        }
    }

    /// <summary>
    /// 浮窗按钮点击事件
    /// </summary>
    private async void OnFloatingButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is FloatingButtonInfo info)
        {
            await info.ToggleAction();
        }
    }

    /// <summary>
    /// 刷新浮窗按钮UI状态
    /// </summary>
    private void RefreshFloatingButtonsUI()
    {
        // 更新各按钮的可见性状态
        foreach (var info in _floatingButtons)
        {
            // 获取最新的可见性状态
            if (info.Id == "alert")
            {
                info.IsVisible = _floatingWindowService.IsVisible;
            }
            else if (info.Id == "logconsole")
            {
                info.IsVisible = _logConsoleWindowService.IsVisible;
            }
            else if (info.Id.StartsWith("stat_"))
            {
                var configIdStr = info.Id.Replace("stat_", "");
                if (int.TryParse(configIdStr, out var configId))
                {
                    info.IsVisible = _statFloatingWindowService.IsVisible(configId);
                }
            }

            // 更新按钮外观
            if (info.Button != null)
            {
                UpdateButtonAppearance(info.Button, info);
            }
        }
    }

    /// <summary>
    /// 显示所有浮窗
    /// </summary>
    private async void BtnShowAllFloating_Click(object sender, RoutedEventArgs e)
    {
        await _floatingWindowService.ShowAsync();
        await _logConsoleWindowService.ShowAsync();
        await _statFloatingWindowService.ShowAllAsync();
    }

    /// <summary>
    /// 隐藏所有浮窗
    /// </summary>
    private void BtnHideAllFloating_Click(object sender, RoutedEventArgs e)
    {
        _floatingWindowService.Hide();
        _logConsoleWindowService.Hide();
        _statFloatingWindowService.HideAll();
    }

    /// <summary>
    /// 刷新应用资源统计
    /// </summary>
    private void RefreshResourceStats()
    {
        try
        {
            _currentProcess.Refresh();

            // 内存占用（工作集）
            var memoryMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0;
            TxtMemoryUsage.Text = memoryMB < 1024
                ? $"{memoryMB:F1}MB"
                : $"{memoryMB / 1024:F2}GB";

            // GC托管内存
            var gcMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            TxtGcMemory.Text = gcMemoryMB < 1024
                ? $"{gcMemoryMB:F1}MB"
                : $"{gcMemoryMB / 1024:F2}GB";

            // CPU使用率
            var currentCpuTime = _currentProcess.TotalProcessorTime;
            var currentTime = DateTime.UtcNow;
            var cpuUsedMs = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
            var totalMsPassed = (currentTime - _lastCpuCheckTime).TotalMilliseconds;
            var cpuUsagePercent = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;
            TxtCpuUsage.Text = $"{cpuUsagePercent:F1}%";
            _lastCpuTime = currentCpuTime;
            _lastCpuCheckTime = currentTime;

            // 线程数
            TxtThreadCount.Text = _currentProcess.Threads.Count.ToString();

            // 句柄数
            TxtHandleCount.Text = _currentProcess.HandleCount.ToString();

            // 运行时间
            var uptime = DateTime.Now - _currentProcess.StartTime;
            TxtUptime.Text = FormatUptime(uptime);
        }
        catch
        {
            // 忽略刷新错误
        }
    }

    /// <summary>
    /// 格式化运行时间
    /// </summary>
    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}天{uptime.Hours}时";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}时{uptime.Minutes}分";
        if (uptime.TotalMinutes >= 1)
            return $"{(int)uptime.TotalMinutes}分{uptime.Seconds}秒";
        return $"{uptime.Seconds}秒";
    }

    /// <summary>
    /// 查看当前预警按钮点击事件
    /// </summary>
    private void BtnCurrentAlerts_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo("CurrentAlerts");
    }

    /// <summary>
    /// 管理预警规则按钮点击事件
    /// </summary>
    private void BtnAlertRules_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo("AlertRules");
    }

    /// <summary>
    /// 数据库连接按钮点击事件
    /// </summary>
    private void BtnDbConnections_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo("DbConnections");
    }

    /// <summary>
    /// 线程数点击事件 - 显示线程明细
    /// </summary>
    private void ThreadCount_Click(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("线程数卡片被点击");
        try
        {
            _currentProcess.Refresh();
            var threads = _currentProcess.Threads;
            Debug.WriteLine($"获取到 {threads.Count} 个线程");

            var sb = new StringBuilder();
            sb.AppendLine($"进程线程总数: {threads.Count}");
            sb.AppendLine($"进程名称: {_currentProcess.ProcessName} (PID: {_currentProcess.Id})");
            sb.AppendLine(new string('═', 80));
            sb.AppendLine();

            // 按状态分组统计
            var stateGroups = new Dictionary<System.Diagnostics.ThreadState, int>();
            var waitReasonGroups = new Dictionary<string, int>();

            foreach (ProcessThread thread in threads)
            {
                try
                {
                    if (!stateGroups.ContainsKey(thread.ThreadState))
                        stateGroups[thread.ThreadState] = 0;
                    stateGroups[thread.ThreadState]++;

                    // 统计等待原因
                    if (thread.ThreadState == System.Diagnostics.ThreadState.Wait)
                    {
                        var reason = GetWaitReasonText(thread.WaitReason);
                        if (!waitReasonGroups.ContainsKey(reason))
                            waitReasonGroups[reason] = 0;
                        waitReasonGroups[reason]++;
                    }
                }
                catch { }
            }

            sb.AppendLine("【状态统计】");
            foreach (var group in stateGroups.OrderByDescending(g => g.Value))
            {
                sb.AppendLine($"  {GetThreadStateText(group.Key),-10}: {group.Value,4} 个");
            }
            sb.AppendLine();

            if (waitReasonGroups.Count > 0)
            {
                sb.AppendLine("【等待原因统计】");
                foreach (var group in waitReasonGroups.OrderByDescending(g => g.Value))
                {
                    sb.AppendLine($"  {group.Key,-20}: {group.Value,4} 个");
                }
                sb.AppendLine();
            }

            // 收集线程信息并按CPU时间排序
            var threadInfos = new List<(int Id, string State, int Priority, TimeSpan CpuTime, TimeSpan UserTime, TimeSpan KernelTime, DateTime StartTime, string WaitReason)>();
            foreach (ProcessThread thread in threads)
            {
                try
                {
                    var waitReason = thread.ThreadState == System.Diagnostics.ThreadState.Wait
                        ? GetWaitReasonText(thread.WaitReason)
                        : "-";

                    threadInfos.Add((
                        thread.Id,
                        GetThreadStateText(thread.ThreadState),
                        thread.BasePriority,
                        thread.TotalProcessorTime,
                        thread.UserProcessorTime,
                        thread.PrivilegedProcessorTime,
                        thread.StartTime,
                        waitReason
                    ));
                }
                catch { }
            }

            // 按CPU时间排序
            var sortedThreads = threadInfos.OrderByDescending(t => t.CpuTime.TotalMilliseconds).ToList();

            sb.AppendLine("【Top 20 高CPU占用线程】");
            sb.AppendLine($"{"ID",-8} {"状态",-10} {"优先级",-6} {"总CPU",-12} {"用户",-12} {"内核",-12} {"启动时间",-20} {"等待原因"}");
            sb.AppendLine(new string('─', 120));

            foreach (var t in sortedThreads.Take(20))
            {
                var startTime = t.StartTime.ToString("MM-dd HH:mm:ss");
                sb.AppendLine($"{t.Id,-8} {t.State,-10} {t.Priority,-6} {t.CpuTime.TotalSeconds,11:F3}s {t.UserTime.TotalSeconds,11:F3}s {t.KernelTime.TotalSeconds,11:F3}s {startTime,-20} {t.WaitReason}");
            }
            sb.AppendLine();

            sb.AppendLine("【全部线程列表】");
            sb.AppendLine($"{"ID",-8} {"状态",-10} {"优先级",-6} {"CPU时间",-12} {"启动时间",-20} {"等待原因"}");
            sb.AppendLine(new string('─', 80));

            foreach (var t in sortedThreads)
            {
                var startTime = t.StartTime.ToString("MM-dd HH:mm:ss");
                sb.AppendLine($"{t.Id,-8} {t.State,-10} {t.Priority,-6} {t.CpuTime.TotalSeconds,11:F3}s {startTime,-20} {t.WaitReason}");
            }

            Debug.WriteLine("准备显示对话框");
            ShowDetailDialog("线程明细", sb.ToString());
            Debug.WriteLine("对话框已显示");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示线程信息失败: {ex.Message}");
            System.Windows.MessageBox.Show($"获取线程信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 句柄数点击事件 - 显示句柄明细
    /// </summary>
    private void HandleCount_Click(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("句柄数卡片被点击");
        try
        {
            _currentProcess.Refresh();

            var sb = new StringBuilder();
            sb.AppendLine($"进程句柄总数: {_currentProcess.HandleCount}");
            sb.AppendLine($"进程名称: {_currentProcess.ProcessName} (PID: {_currentProcess.Id})");
            sb.AppendLine(new string('═', 80));
            sb.AppendLine();

            // GDI和User对象数量
            var gdiObjects = GetGuiResources(_currentProcess.Handle, 0);
            var userObjects = GetGuiResources(_currentProcess.Handle, 1);

            sb.AppendLine("【GUI资源统计】");
            sb.AppendLine($"  GDI对象数量  : {gdiObjects}");
            sb.AppendLine($"  User对象数量 : {userObjects}");
            sb.AppendLine($"  总GUI对象    : {gdiObjects + userObjects}");
            sb.AppendLine();

            sb.AppendLine("【内存统计】");
            sb.AppendLine($"  工作集内存   : {_currentProcess.WorkingSet64 / 1024.0 / 1024.0,10:F2} MB  (实际占用的物理内存)");
            sb.AppendLine($"  私有内存     : {_currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0,10:F2} MB  (进程独占的内存)");
            sb.AppendLine($"  虚拟内存     : {_currentProcess.VirtualMemorySize64 / 1024.0 / 1024.0,10:F2} MB  (虚拟地址空间)");
            sb.AppendLine($"  分页内存     : {_currentProcess.PagedMemorySize64 / 1024.0 / 1024.0,10:F2} MB  (可交换到磁盘的内存)");
            sb.AppendLine($"  非分页内存   : {_currentProcess.NonpagedSystemMemorySize64 / 1024.0 / 1024.0,10:F2} MB  (必须保持在物理内存中)");
            sb.AppendLine($"  峰值工作集   : {_currentProcess.PeakWorkingSet64 / 1024.0 / 1024.0,10:F2} MB");
            sb.AppendLine($"  峰值虚拟内存 : {_currentProcess.PeakVirtualMemorySize64 / 1024.0 / 1024.0,10:F2} MB");
            sb.AppendLine();

            sb.AppendLine("【进程信息】");
            sb.AppendLine($"  进程ID       : {_currentProcess.Id}");
            sb.AppendLine($"  进程名称     : {_currentProcess.ProcessName}");
            sb.AppendLine($"  启动时间     : {_currentProcess.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  运行时长     : {(DateTime.Now - _currentProcess.StartTime).ToString(@"dd\.hh\:mm\:ss")}");
            sb.AppendLine($"  基础优先级   : {_currentProcess.BasePriority}");
            sb.AppendLine($"  线程数       : {_currentProcess.Threads.Count}");
            sb.AppendLine($"  句柄数       : {_currentProcess.HandleCount}");
            sb.AppendLine();

            sb.AppendLine("【已加载模块】");
            sb.AppendLine($"  模块总数     : {_currentProcess.Modules.Count}");
            sb.AppendLine();

            // 按模块大小排序，显示前20个
            var modules = new List<(string Name, long Size, string FileName)>();
            foreach (ProcessModule module in _currentProcess.Modules)
            {
                try
                {
                    modules.Add((module.ModuleName, module.ModuleMemorySize, module.FileName));
                }
                catch { }
            }

            var sortedModules = modules.OrderByDescending(m => m.Size).ToList();

            sb.AppendLine("【Top 20 内存占用模块】");
            sb.AppendLine($"{"模块名称",-40} {"大小",-12} {"完整路径"}");
            sb.AppendLine(new string('─', 120));

            foreach (var module in sortedModules.Take(20))
            {
                var sizeMB = module.Size / 1024.0 / 1024.0;
                sb.AppendLine($"{module.Name,-40} {sizeMB,11:F2} MB {module.FileName}");
            }
            sb.AppendLine();

            sb.AppendLine("【全部已加载模块】");
            sb.AppendLine($"{"模块名称",-40} {"大小",-12}");
            sb.AppendLine(new string('─', 60));

            foreach (var module in sortedModules)
            {
                var sizeMB = module.Size / 1024.0 / 1024.0;
                sb.AppendLine($"{module.Name,-40} {sizeMB,11:F2} MB");
            }

            Debug.WriteLine("准备显示句柄明细对话框");
            ShowDetailDialog("句柄明细", sb.ToString());
            Debug.WriteLine("句柄明细对话框已显示");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示句柄信息失败: {ex.Message}");
            System.Windows.MessageBox.Show($"获取句柄信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示明细对话框
    /// </summary>
    private void ShowDetailDialog(string title, string content)
    {
        var textBox = new System.Windows.Controls.TextBox
        {
            Text = content,
            IsReadOnly = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas, 微软雅黑"),
            FontSize = 11,
            Background = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            TextWrapping = TextWrapping.NoWrap,
            Padding = new Thickness(10)
        };

        var scrollViewer = new System.Windows.Controls.ScrollViewer
        {
            Content = textBox,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Height = 600,
            Width = 900
        };

        var window = new System.Windows.Window
        {
            Title = title,
            Content = scrollViewer,
            Width = 950,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.CanResize,
            Background = System.Windows.Media.Brushes.White
        };

        // 尝试设置Owner，失败则忽略
        try
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded)
            {
                window.Owner = mainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }
        catch
        {
            Debug.WriteLine("无法设置Owner，使用屏幕居中");
        }

        Debug.WriteLine($"显示弹窗: {title}");
        window.ShowDialog();
    }

    /// <summary>
    /// 获取线程状态文本
    /// </summary>
    private static string GetThreadStateText(System.Diagnostics.ThreadState state)
    {
        return state switch
        {
            System.Diagnostics.ThreadState.Running => "运行中",
            System.Diagnostics.ThreadState.Ready => "就绪",
            System.Diagnostics.ThreadState.Wait => "等待",
            System.Diagnostics.ThreadState.Standby => "待命",
            System.Diagnostics.ThreadState.Terminated => "已终止",
            System.Diagnostics.ThreadState.Transition => "转换中",
            System.Diagnostics.ThreadState.Unknown => "未知",
            _ => state.ToString()
        };
    }

    /// <summary>
    /// 获取等待原因文本
    /// </summary>
    private static string GetWaitReasonText(ThreadWaitReason reason)
    {
        return reason switch
        {
            ThreadWaitReason.Executive => "执行体",
            ThreadWaitReason.FreePage => "空闲页",
            ThreadWaitReason.PageIn => "页面换入",
            ThreadWaitReason.SystemAllocation => "系统分配",
            ThreadWaitReason.ExecutionDelay => "执行延迟",
            ThreadWaitReason.Suspended => "已挂起",
            ThreadWaitReason.UserRequest => "用户请求",
            ThreadWaitReason.EventPairHigh => "事件对高",
            ThreadWaitReason.EventPairLow => "事件对低",
            ThreadWaitReason.LpcReceive => "LPC接收",
            ThreadWaitReason.LpcReply => "LPC回复",
            ThreadWaitReason.VirtualMemory => "虚拟内存",
            ThreadWaitReason.PageOut => "页面换出",
            _ => reason.ToString()
        };
    }

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

    /// <summary>
    /// 编辑模式变化事件处理
    /// </summary>
    private void OnEditModeChanged(object? sender, bool isEditMode)
    {
        Dispatcher.Invoke(() => UpdateEditModeButton(isEditMode));
    }

    /// <summary>
    /// 编辑模式按钮点击事件
    /// </summary>
    private void BtnEditMode_Click(object sender, RoutedEventArgs e)
    {
        _editModeService.ToggleEditMode();
    }

    /// <summary>
    /// 更新编辑模式按钮状态
    /// </summary>
    private void UpdateEditModeButton(bool isEditMode)
    {
        if (FindName("BtnEditMode") is not Button btnEditMode) return;

        if (isEditMode)
        {
            btnEditMode.Content = "退出编辑";
            btnEditMode.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
            btnEditMode.Foreground = System.Windows.Media.Brushes.White;
        }
        else
        {
            btnEditMode.Content = "编辑浮窗";
            btnEditMode.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light gray
            btnEditMode.Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)); // Dark gray
        }
    }
}
