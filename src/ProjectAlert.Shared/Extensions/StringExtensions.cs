namespace ProjectAlert.Shared.Extensions;

/// <summary>
/// 字符串扩展方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 判断字符串是否为空或仅包含空白字符
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 判断字符串是否有值
    /// </summary>
    public static bool HasValue(this string? value)
        => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 将字符串转换为指定类型
    /// </summary>
    public static T? To<T>(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        try
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsEnum)
                return (T)Enum.Parse(underlyingType, value, true);

            return (T)Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 截取字符串
    /// </summary>
    public static string Truncate(this string? value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;

        return value[..(maxLength - suffix.Length)] + suffix;
    }
}
