using ProjectAlert.Domain.Entities;

namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 应用设置仓储接口
/// </summary>
public interface IAppSettingRepository
{
    /// <summary>
    /// 获取所有应用设置
    /// </summary>
    /// <returns>应用设置集合</returns>
    Task<IEnumerable<AppSetting>> GetAllAsync();

    /// <summary>
    /// 根据键名获取设置
    /// </summary>
    /// <param name="key">配置键名</param>
    /// <returns>应用设置对象</returns>
    Task<AppSetting?> GetByKeyAsync(string key);

    /// <summary>
    /// 根据键名获取配置值
    /// </summary>
    /// <param name="key">配置键名</param>
    /// <returns>配置值</returns>
    Task<string?> GetValueAsync(string key);

    /// <summary>
    /// 设置配置值
    /// </summary>
    /// <param name="key">配置键名</param>
    /// <param name="value">配置值</param>
    /// <param name="description">配置描述</param>
    Task SetValueAsync(string key, string value, string? description = null);
}
