using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Scanner.App.Converters;

/// <summary>
/// Converts null to Collapsed and non-null to Visible.
/// Use ConverterParameter="Invert" to invert the behavior.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNull = value == null;
        var invert = parameter is string str && str.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
            isNull = !isNull;

        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to Visibility.
/// Use ConverterParameter="Invert" to invert the behavior.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolValue = value is bool b && b;
        var invert = parameter is string str && str.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null to false and non-null to true.
/// Use ConverterParameter="Invert" to invert the behavior.
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNotNull = value != null;
        var invert = parameter is string str && str.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
            isNotNull = !isNotNull;

        return isNotNull;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
