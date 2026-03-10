using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using OpenSysKit.UI.ViewModels;

namespace OpenSysKit.UI.Converters;

/// <summary>Converts bool to one of two colors. ConverterParameter="TrueColor|FalseColor"</summary>
public class BooleanToColorConverter : IValueConverter
{
    public static readonly BooleanToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool bv && bv;
        string param = parameter as string ?? "#FFFFFF|#888888";
        string[] parts = param.Split('|');
        string hex = b ? parts[0] : (parts.Length > 1 ? parts[1] : "#888888");
        return Color.Parse(hex);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Compares a NavPage to a string parameter for page visibility</summary>
public class PageEqualityConverter : IValueConverter
{
    public static readonly PageEqualityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NavPage page && parameter is string s)
            return page.ToString() == s;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns "ok" color, "degraded" warn, "error" error</summary>
public class HealthStatusColorConverter : IValueConverter
{
    public static readonly HealthStatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "ok" => Color.Parse("#3DD68C"),
            "degraded" => Color.Parse("#F5A623"),
            "error" => Color.Parse("#E05C5C"),
            _ => Color.Parse("#4A6580"),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Service state to color</summary>
public class ServiceStateColorConverter : IValueConverter
{
    public static readonly ServiceStateColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "running" => new SolidColorBrush(Color.Parse("#3DD68C")),
            "stopped" => new SolidColorBrush(Color.Parse("#4A6580")),
            _ => new SolidColorBrush(Color.Parse("#F5A623")),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
