namespace ProjectAlert.Shared.Extensions;

/// <summary>
/// 日期时间扩展方法
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// 获取友好的相对时间描述
    /// </summary>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var span = DateTime.Now - dateTime;

        if (span.TotalSeconds < 60)
            return "刚刚";
        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes}分钟前";
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours}小时前";
        if (span.TotalDays < 7)
            return $"{(int)span.TotalDays}天前";
        if (span.TotalDays < 30)
            return $"{(int)(span.TotalDays / 7)}周前";
        if (span.TotalDays < 365)
            return $"{(int)(span.TotalDays / 30)}月前";

        return $"{(int)(span.TotalDays / 365)}年前";
    }

    /// <summary>
    /// 格式化为标准日期时间字符串
    /// </summary>
    public static string ToStandardString(this DateTime dateTime)
        => dateTime.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 格式化为时间字符串
    /// </summary>
    public static string ToTimeString(this DateTime dateTime)
        => dateTime.ToString("HH:mm");

    /// <summary>
    /// 获取当天开始时间
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
        => dateTime.Date;

    /// <summary>
    /// 获取当天结束时间
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
        => dateTime.Date.AddDays(1).AddTicks(-1);
}
