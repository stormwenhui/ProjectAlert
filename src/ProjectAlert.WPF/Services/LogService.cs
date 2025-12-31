using System.Collections.ObjectModel;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 全局日志服务
/// </summary>
public class LogService
{
    private readonly object _lock = new();
    private const int MaxLogCount = 500;

    /// <summary>
    /// 日志条目集合
    /// </summary>
    public ObservableCollection<LogEntry> Logs { get; } = [];

    /// <summary>
    /// 新日志事件
    /// </summary>
    public event Action<LogEntry>? LogAdded;

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public void Debug(string source, string message) => Log(LogLevel.Debug, source, message);

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public void Info(string source, string message) => Log(LogLevel.Info, source, message);

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public void Warning(string source, string message) => Log(LogLevel.Warning, source, message);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public void Error(string source, string message) => Log(LogLevel.Error, source, message);

    /// <summary>
    /// 记录日志
    /// </summary>
    public void Log(LogLevel level, string source, string message)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Level = level,
            Source = source,
            Message = message
        };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                Logs.Add(entry);

                // 限制日志数量
                while (Logs.Count > MaxLogCount)
                {
                    Logs.RemoveAt(0);
                }
            }

            LogAdded?.Invoke(entry);
        });
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    public void Clear()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                Logs.Clear();
            }
        });
    }
}
