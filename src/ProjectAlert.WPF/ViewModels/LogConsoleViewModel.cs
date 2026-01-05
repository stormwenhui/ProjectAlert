using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.WPF.Services;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 日志级别筛选项
/// </summary>
public partial class LogLevelFilter : ObservableObject
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isChecked = true;
}

/// <summary>
/// 日志控制台视图模型
/// </summary>
public partial class LogConsoleViewModel : FloatingWidgetViewModelBase
{
    private readonly LogService _logService;
    private readonly System.Collections.Specialized.NotifyCollectionChangedEventHandler _collectionChangedHandler;
    private readonly PropertyChangedEventHandler _filterPropertyChangedHandler;

    /// <summary>
    /// 日志列表视图
    /// </summary>
    public ICollectionView LogsView { get; }

    /// <summary>
    /// 日志级别筛选列表
    /// </summary>
    public ObservableCollection<LogLevelFilter> LevelFilters { get; } = [];

    /// <summary>
    /// 是否自动滚动
    /// </summary>
    [ObservableProperty]
    private bool _autoScroll = true;

    /// <summary>
    /// 日志数量
    /// </summary>
    [ObservableProperty]
    private int _logCount;

    /// <summary>
    /// 筛选后的日志数量
    /// </summary>
    [ObservableProperty]
    private int _filteredLogCount;

    /// <summary>
    /// 搜索关键字
    /// </summary>
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    /// <summary>
    /// 新日志事件（用于通知UI滚动）
    /// </summary>
    public event Action? NewLogAdded;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LogConsoleViewModel(LogService logService)
    {
        _logService = logService;
        Title = "日志控制台";

        // 创建日志视图
        LogsView = CollectionViewSource.GetDefaultView(_logService.Logs);
        LogsView.Filter = FilterLog;

        // 初始化级别筛选
        LevelFilters.Add(new LogLevelFilter { Level = LogLevel.Debug, Name = "调试", IsChecked = false });
        LevelFilters.Add(new LogLevelFilter { Level = LogLevel.Info, Name = "信息", IsChecked = true });
        LevelFilters.Add(new LogLevelFilter { Level = LogLevel.Warning, Name = "警告", IsChecked = true });
        LevelFilters.Add(new LogLevelFilter { Level = LogLevel.Error, Name = "错误", IsChecked = true });

        // 使用命名委托以便后续取消订阅
        _filterPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(LogLevelFilter.IsChecked))
            {
                LogsView.Refresh();
                UpdateFilteredCount();
            }
        };

        foreach (var filter in LevelFilters)
        {
            filter.PropertyChanged += _filterPropertyChangedHandler;
        }

        // 监听新日志
        _logService.LogAdded += OnLogAdded;

        // 监听集合变化
        _collectionChangedHandler = (s, e) => LogCount = _logService.Logs.Count;
        _logService.Logs.CollectionChanged += _collectionChangedHandler;

        LogCount = _logService.Logs.Count;
        UpdateFilteredCount();
    }

    /// <summary>
    /// 日志筛选器
    /// </summary>
    private bool FilterLog(object obj)
    {
        if (obj is not LogEntry log)
            return false;

        // 级别筛选
        var levelFilter = LevelFilters.FirstOrDefault(f => f.Level == log.Level);
        if (levelFilter != null && !levelFilter.IsChecked)
            return false;

        // 关键字筛选
        if (!string.IsNullOrEmpty(SearchKeyword))
        {
            var keyword = SearchKeyword.ToLower();
            if (!log.Message.ToLower().Contains(keyword) &&
                !log.Source.ToLower().Contains(keyword))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 新日志添加处理
    /// </summary>
    private void OnLogAdded(LogEntry entry)
    {
        LogsView.Refresh();
        UpdateFilteredCount();
        LastUpdateTime = DateTime.Now;

        if (AutoScroll)
        {
            NewLogAdded?.Invoke();
        }
    }

    /// <summary>
    /// 更新筛选后数量
    /// </summary>
    private void UpdateFilteredCount()
    {
        FilteredLogCount = LogsView.Cast<LogEntry>().Count();
    }

    /// <summary>
    /// 搜索关键字变化时刷新
    /// </summary>
    partial void OnSearchKeywordChanged(string value)
    {
        LogsView.Refresh();
        UpdateFilteredCount();
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        _logService.Clear();
        UpdateFilteredCount();
    }

    /// <summary>
    /// 复制日志
    /// </summary>
    [RelayCommand]
    private void CopyLog(LogEntry? log)
    {
        if (log == null) return;
        try
        {
            var text = $"{log.TimeText} [{log.Source}] {log.Message}";
            System.Windows.Clipboard.SetText(text);
        }
        catch
        {
            // 忽略剪贴板错误
        }
    }

    /// <summary>
    /// 刷新
    /// </summary>
    public override Task RefreshAsync()
    {
        LogsView.Refresh();
        UpdateFilteredCount();
        LastUpdateTime = DateTime.Now;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 取消日志服务事件订阅
            _logService.LogAdded -= OnLogAdded;
            _logService.Logs.CollectionChanged -= _collectionChangedHandler;

            // 取消筛选器事件订阅
            foreach (var filter in LevelFilters)
            {
                filter.PropertyChanged -= _filterPropertyChangedHandler;
            }
        }

        base.Dispose(disposing);
    }
}
