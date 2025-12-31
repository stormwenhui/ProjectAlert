using System.Globalization;
using System.Windows.Data;

namespace ProjectAlert.WPF.Converters;

/// <summary>
/// 可空枚举显示转换器
/// null 显示参数指定的文本，枚举值显示枚举名称
/// </summary>
public class NullableEnumDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return parameter?.ToString() ?? "全部";
        }
        return value.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
