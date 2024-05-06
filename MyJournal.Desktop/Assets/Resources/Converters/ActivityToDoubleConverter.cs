using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using MyJournal.Core.UserData;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class ActivityToDoubleConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not Activity.Statuses activity)
			return 0;

		return System.Convert.ToDouble(value: activity == Activity.Statuses.Online);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);
}