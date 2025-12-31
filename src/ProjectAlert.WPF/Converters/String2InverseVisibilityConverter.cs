using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjectAlert.WPF.Converters;

/// <summary>
/// 字符串转反向可见性转换器
/// 空字符串时可见，非空时隐藏
/// </summary>
public class String2InverseVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
