using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenSysKit.UI.Converters;

public sealed class BooleanToGlyphChevronConverter : IValueConverter
{
    public static readonly BooleanToGlyphChevronConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isExpanded && isExpanded ? "−" : "+";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool booleanValue ? !booleanValue : true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
