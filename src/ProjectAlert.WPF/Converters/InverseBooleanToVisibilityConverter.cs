using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjectAlert.WPF.Converters;

/// <summary>
/// 反向布尔值转可见性转换器
/// true 返回 Collapsed，false 返回 Visible
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
