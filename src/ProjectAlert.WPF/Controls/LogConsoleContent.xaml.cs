using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Controls;

/// <summary>
/// 日志控制台内容控件
/// </summary>
public partial class LogConsoleContent : UserControl
{
    public LogConsoleContent()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is LogConsoleViewModel viewModel)
        {
            // 订阅新日志事件以自动滚动
            viewModel.NewLogAdded += OnNewLogAdded;
        }

        if (e.OldValue is LogConsoleViewModel oldViewModel)
        {
            oldViewModel.NewLogAdded -= OnNewLogAdded;
        }
    }

    /// <summary>
    /// 新日志添加时滚动到底部
    /// </summary>
    private void OnNewLogAdded()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (LogListBox.Items.Count > 0)
            {
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
            }
        });
    }
}
