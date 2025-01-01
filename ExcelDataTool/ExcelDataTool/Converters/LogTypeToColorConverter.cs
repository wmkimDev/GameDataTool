using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ExcelDataTool.Core;

namespace ExcelDataTool.Converters;

public class LogTypeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is LogMessageType type)
        {
            return type switch
            {
                LogMessageType.Info    => Brushes.Black,
                LogMessageType.Warning => Brushes.DarkOrange,
                LogMessageType.Error   => Brushes.Red,
                LogMessageType.Success => Brushes.ForestGreen,
                _                      => Brushes.Black
            };
        }
        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}