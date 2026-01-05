using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectAlert.WPF.ViewModels;

/// <summary>
/// 桌面浮窗小组件基类 ViewModel
/// </summary>
public abstract partial class FloatingWidgetViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// 窗口标题
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [ObservableProperty]
    private DateTime _lastUpdateTime = DateTime.Now;

    /// <summary>
    /// 是否锁定（锁定后无法拖拽）
    /// </summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary>
    /// 是否置顶
    /// </summary>
    [ObservableProperty]
    private bool _isTopmost = true;

    /// <summary>
    /// 窗口透明度 (0.0 - 1.0)，默认 0.7 (即 30% 透明)
    /// </summary>
    [ObservableProperty]
    private double _windowOpacity = 0.7;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 错误信息
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 刷新数据
    /// </summary>
    public abstract Task RefreshAsync();

    /// <summary>
    /// 通知 HasError 属性变化
    /// </summary>
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源（子类可重写）
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
    }
}
