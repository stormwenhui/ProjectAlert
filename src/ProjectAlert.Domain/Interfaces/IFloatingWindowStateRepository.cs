using ProjectAlert.Domain.Entities;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 浮窗状态仓储接口
/// </summary>
public interface IFloatingWindowStateRepository
{
    /// <summary>
    /// 获取所有浮窗状态
    /// </summary>
    Task<IEnumerable<FloatingWindowState>> GetAllAsync();

    /// <summary>
    /// 根据窗口ID获取状态
    /// </summary>
    Task<FloatingWindowState?> GetByIdAsync(string windowId);

    /// <summary>
    /// 保存浮窗状态（存在则更新，不存在则插入）
    /// </summary>
    Task SaveAsync(FloatingWindowState state);

    /// <summary>
    /// 删除浮窗状态
    /// </summary>
    Task DeleteAsync(string windowId);
}
