using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    /// <summary>
    /// 日志时间
    /// </summary>
    public DateTime Time { get; set; } = DateTime.Now;

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Info;

    /// <summary>
    /// 日志来源（如：系统、API、定时任务等）
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 格式化的时间文本
    /// </summary>
    public string TimeText => Time.ToString("HH:mm:ss");

    /// <summary>
    /// 级别简称
    /// </summary>
    public string LevelShort => Level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "UNK"
    };
}
