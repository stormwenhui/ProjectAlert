using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ProjectAlert.Domain.Interfaces;
using WpfApplication = System.Windows.Application;
using DrawingColor = System.Drawing.Color;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 系统托盘图标服务
/// </summary>
public class TrayIconService : IDisposable
{
    private readonly ICurrentAlertRepository _currentAlertRepository;
    private NotifyIcon? _notifyIcon;
    private Icon? _normalIcon;
    private Icon? _alertIcon;
    private bool _disposed;

    /// <summary>
    /// 显示主窗口事件
    /// </summary>
    public event Action? ShowMainWindowRequested;

    /// <summary>
    /// 退出应用事件
    /// </summary>
    public event Action? ExitRequested;

    /// <summary>
    /// 显示悬浮窗事件
    /// </summary>
    public event Action? ToggleFloatingWindowRequested;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TrayIconService(ICurrentAlertRepository currentAlertRepository)
    {
        _currentAlertRepository = currentAlertRepository;
    }

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    public void Initialize()
    {
        if (_notifyIcon != null) return;

        // 创建图标
        _normalIcon = CreateTextIcon("文", DrawingColor.FromArgb(33, 150, 243), DrawingColor.White);
        _alertIcon = CreateTextIcon("文", DrawingColor.FromArgb(244, 67, 54), DrawingColor.White);

        // 创建托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = _normalIcon,
            Text = "项目监控",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        // 双击显示主窗口
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindowRequested?.Invoke();
    }

    /// <summary>
    /// 创建文字图标（艺术字风格）
    /// </summary>
    private static Icon CreateTextIcon(string text, DrawingColor backgroundColor, DrawingColor textColor)
    {
        const int size = 64;

        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);

        // 高质量渲染
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // 绘制圆角矩形背景
        using var backgroundBrush = new SolidBrush(backgroundColor);
        var rect = new Rectangle(2, 2, size - 4, size - 4);
        var radius = 12;
        using var path = CreateRoundedRectangle(rect, radius);
        graphics.FillPath(backgroundBrush, path);

        // 添加轻微阴影效果（通过叠加半透明黑色）
        using var shadowPath = CreateRoundedRectangle(new Rectangle(4, 4, size - 4, size - 4), radius);
        using var shadowBrush = new SolidBrush(DrawingColor.FromArgb(30, 0, 0, 0));
        graphics.FillPath(shadowBrush, shadowPath);

        // 设置字体 - 使用粗体宋体呈现艺术字效果
        using var font = new Font("Microsoft YaHei", 36, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(textColor);

        // 计算文字位置（居中）
        var textSize = graphics.MeasureString(text, font);
        var x = (size - textSize.Width) / 2;
        var y = (size - textSize.Height) / 2 - 2; // 微调垂直位置

        // 绘制文字阴影
        using var shadowTextBrush = new SolidBrush(DrawingColor.FromArgb(60, 0, 0, 0));
        graphics.DrawString(text, font, shadowTextBrush, x + 1, y + 1);

        // 绘制文字
        graphics.DrawString(text, font, textBrush, x, y);

        // 转换为图标
        var hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    /// <summary>
    /// 创建圆角矩形路径
    /// </summary>
    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    /// <summary>
    /// 创建右键菜单
    /// </summary>
    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        // 打开主窗口
        var showItem = new ToolStripMenuItem("打开主窗口");
        showItem.Click += (_, _) => ShowMainWindowRequested?.Invoke();
        menu.Items.Add(showItem);

        // 显示/隐藏悬浮窗
        var floatingItem = new ToolStripMenuItem("显示/隐藏悬浮窗");
        floatingItem.Click += (_, _) => ToggleFloatingWindowRequested?.Invoke();
        menu.Items.Add(floatingItem);

        menu.Items.Add(new ToolStripSeparator());

        // 退出
        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// 更新图标状态（根据是否有预警）
    /// </summary>
    public async Task UpdateIconStateAsync()
    {
        if (_notifyIcon == null) return;

        var alerts = await _currentAlertRepository.GetActiveAlertsAsync();
        var hasAlerts = alerts.Any();

        WpfApplication.Current?.Dispatcher.Invoke(() =>
        {
            _notifyIcon.Icon = hasAlerts ? _alertIcon : _normalIcon;
            _notifyIcon.Text = hasAlerts ? $"项目监控 - {alerts.Count()} 条预警" : "项目监控";
        });
    }

    /// <summary>
    /// 设置图标为有预警状态
    /// </summary>
    public void SetAlertState(bool hasAlert, int alertCount = 0)
    {
        if (_notifyIcon == null) return;

        WpfApplication.Current?.Dispatcher.Invoke(() =>
        {
            _notifyIcon.Icon = hasAlert ? _alertIcon : _normalIcon;
            _notifyIcon.Text = hasAlert ? $"项目监控 - {alertCount} 条预警" : "项目监控";
        });
    }

    /// <summary>
    /// 显示气泡提示
    /// </summary>
    public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        _notifyIcon?.ShowBalloonTip(timeout, title, text, icon);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _normalIcon?.Dispose();
        _alertIcon?.Dispose();
    }
}
