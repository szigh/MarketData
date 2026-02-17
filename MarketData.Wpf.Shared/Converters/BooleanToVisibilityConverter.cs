using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarketData.Wpf.Shared.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Inverted { get; set; }
    public bool UseHidden { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;

        if (Inverted)
            boolValue = !boolValue;

        if (boolValue)
            return Visibility.Visible;

        return UseHidden ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool result = value is Visibility visibility && visibility == Visibility.Visible;
        
        if (Inverted)
            result = !result;

        return result;
    }
}
