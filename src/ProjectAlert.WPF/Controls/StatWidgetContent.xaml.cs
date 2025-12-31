using System.Windows.Controls;
using ProjectAlert.WPF.ViewModels;

namespace ProjectAlert.WPF.Controls;

/// <summary>
/// 统计小组件内容控件
/// </summary>
public partial class StatWidgetContent : UserControl
{
    public StatWidgetContent()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is StatFloatingViewModel viewModel)
        {
            // 订阅列变化事件以动态生成列
            viewModel.Columns.CollectionChanged += (s, args) => GenerateColumns(viewModel);
        }
    }

    /// <summary>
    /// 生成DataGrid列
    /// </summary>
    private void GenerateColumns(StatFloatingViewModel viewModel)
    {
        DataGridStats.Columns.Clear();
        foreach (var column in viewModel.Columns)
        {
            DataGridStats.Columns.Add(new DataGridTextColumn
            {
                Header = column,
                Binding = new System.Windows.Data.Binding($"[{column}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
    }
}
