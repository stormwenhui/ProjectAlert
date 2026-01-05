using System.Collections.ObjectModel;
using System.IO;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.WPF.Services;

/// <summary>
/// 全局日志服务
/// </summary>
public class LogService
{
    private readonly object _lock = new();
    private readonly object _fileLock = new();
    private const int MaxLogCount = 500;
    private const int TrimBatchSize = 50;  // 批量移除数量，减少频繁 UI 更新
    private readonly string _logFilePath;
    private readonly bool _enableFileLog;

    /// <summary>
    /// 日志条目集合
    /// </summary>
    public ObservableCollection<LogEntry> Logs { get; } = [];

    /// <summary>
    /// 新日志事件
    /// </summary>
    public event Action<LogEntry>? LogAdded;

    public LogService()
    {
        // 仅在 DEBUG 模式下启用文件日志
#if DEBUG
        _enableFileLog = true;
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"taskqueue_{DateTime.Now:yyyyMMdd}.log");
#else
        _enableFileLog = false;
        _logFilePath = string.Empty;
#endif
    }

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

        // 写入文件日志（DEBUG 模式）
        if (_enableFileLog)
        {
            WriteToFile(entry);
        }

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                Logs.Add(entry);

                // 批量移除旧日志，减少频繁 UI 更新
                // 当超过最大数量时，一次性移除 TrimBatchSize 条
                if (Logs.Count > MaxLogCount + TrimBatchSize)
                {
                    for (int i = 0; i < TrimBatchSize; i++)
                    {
                        Logs.RemoveAt(0);
                    }
                }
            }

            LogAdded?.Invoke(entry);
        });
    }

    /// <summary>
    /// 记录任务队列详细日志（仅 DEBUG 模式写入文件）
    /// </summary>
    public void TaskQueueLog(string phase, string taskKey, string detail)
    {
        var message = $"[{phase}] {taskKey} - {detail}";

        // 始终写入文件（DEBUG 模式）
        if (_enableFileLog)
        {
            var entry = new LogEntry
            {
                Time = DateTime.Now,
                Level = LogLevel.Debug,
                Source = "任务队列",
                Message = message
            };
            WriteToFile(entry);
        }

        // 调试输出
        System.Diagnostics.Debug.WriteLine($"[TaskQueue] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    /// <summary>
    /// 写入日志文件
    /// </summary>
    private void WriteToFile(LogEntry entry)
    {
        try
        {
            var logLine = $"{entry.Time:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level,-7}] [{entry.Source}] {entry.Message}";

            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
        }
        catch
        {
            // 忽略文件写入错误
        }
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
