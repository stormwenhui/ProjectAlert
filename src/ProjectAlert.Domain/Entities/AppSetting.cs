using Dapper.Contrib.Extensions;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 应用设置实体
/// </summary>
[Table("app_settings")]
public class AppSetting
{
    /// <summary>
    /// 配置键
    /// </summary>
    [ExplicitKey]
    public string KeyName { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 配置描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
