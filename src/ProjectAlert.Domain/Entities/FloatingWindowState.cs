using Dapper.Contrib.Extensions;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 浮窗状态实体
/// </summary>
[Table("floating_window_states")]
public class FloatingWindowState
{
    /// <summary>
    /// 浮窗唯一标识（如 "alert" 或 "stat_{configId}"）
    /// </summary>
    [ExplicitKey]
    public string WindowId { get; set; } = string.Empty;

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// 窗口左边位置
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// 窗口顶部位置
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 窗口高度
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 窗口透明度
    /// </summary>
    public double Opacity { get; set; } = 0.7;

    /// <summary>
    /// 是否锁定位置
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsTopmost { get; set; } = true;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}