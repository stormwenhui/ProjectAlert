using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProjectAlert.WPF.Converters;

/// <summary>
/// 布尔值转颜色转换器
/// true 返回激活颜色（绿色），false 返回默认颜色（灰色）
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // 激活状态 - 绿色
                : new SolidColorBrush(Color.FromRgb(176, 190, 197)); // 默认状态 - 灰色
        }
        return new SolidColorBrush(Color.FromRgb(176, 190, 197));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
