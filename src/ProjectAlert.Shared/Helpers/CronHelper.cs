namespace ProjectAlert.Shared.Helpers;

/// <summary>
/// Cron 表达式帮助类
/// </summary>
public static class CronHelper
{
    /// <summary>
    /// 常用 Cron 表达式模板
    /// </summary>
    public static readonly Dictionary<string, (string Expression, string Description)> Templates = new()
    {
        ["every_minute"] = ("0 * * * * ?", "每分钟"),
        ["every_5_minutes"] = ("0 */5 * * * ?", "每5分钟"),
        ["every_10_minutes"] = ("0 */10 * * * ?", "每10分钟"),
        ["every_15_minutes"] = ("0 */15 * * * ?", "每15分钟"),
        ["every_30_minutes"] = ("0 */30 * * * ?", "每30分钟"),
        ["every_hour"] = ("0 0 * * * ?", "每小时"),
        ["every_2_hours"] = ("0 0 */2 * * ?", "每2小时"),
        ["every_day_9am"] = ("0 0 9 * * ?", "每天9点"),
        ["every_day_18pm"] = ("0 0 18 * * ?", "每天18点"),
        ["workday_9am"] = ("0 0 9 ? * MON-FRI", "工作日9点"),
        ["workday_18pm"] = ("0 0 18 ? * MON-FRI", "工作日18点")
    };

    /// <summary>
    /// 获取 Cron 表达式的人类可读描述
    /// </summary>
    public static string GetDescription(string cronExpression)
    {
        foreach (var template in Templates)
        {
            if (template.Value.Expression == cronExpression)
                return template.Value.Description;
        }

        // 简单解析常见模式
        var parts = cronExpression.Split(' ');
        if (parts.Length < 6) return cronExpression;

        // 解析秒
        if (parts[0] == "0" && parts[1].StartsWith("*/"))
        {
            var interval = parts[1][2..];
            return $"每{interval}分钟";
        }

        if (parts[0] == "0" && parts[1] == "0" && parts[2] == "*")
        {
            return "每小时";
        }

        if (parts[0] == "0" && parts[1] == "0" && int.TryParse(parts[2], out var hour))
        {
            return $"每天{hour}点";
        }

        return cronExpression;
    }

    /// <summary>
    /// 验证 Cron 表达式是否有效
    /// </summary>
    public static bool IsValid(string cronExpression)
    {
        try
        {
            var trigger = Quartz.CronExpression.IsValidExpression(cronExpression);
            return trigger;
        }
        catch
        {
            return false;
        }
    }
}
