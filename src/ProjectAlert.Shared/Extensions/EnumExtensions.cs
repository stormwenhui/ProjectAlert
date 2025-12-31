using System.ComponentModel;
using System.Reflection;

namespace ProjectAlert.Shared.Extensions;

/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取枚举的描述信息
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// 将字符串转换为枚举
    /// </summary>
    public static T ToEnum<T>(this string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result))
            return result;
        return default;
    }

    /// <summary>
    /// 将字符串转换为枚举（可空）
    /// </summary>
    public static T? ToEnumOrNull<T>(this string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Enum.TryParse<T>(value, true, out var result))
            return result;
        return null;
    }
}
