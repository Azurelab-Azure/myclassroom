using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PlanPeopleAvalonia.ViewModels;

public class ScoreColorConverter : IValueConverter
{
    public static readonly ScoreColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int delta)
        {
            return delta >= 0 ? Brushes.Green : Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}