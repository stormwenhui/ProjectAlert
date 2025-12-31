using System.Globalization;
using System.Windows.Data;

namespace ProjectAlert.WPF.Converters;

/// <summary>
/// 布尔值转锁定图标转换器
/// true (锁定) 返回锁住图标，false (解锁) 返回开锁图标
/// </summary>
public class BoolToLockIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isLocked)
        {
            return isLocked ? "🔒" : "🔓";
        }
        return "🔓";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
