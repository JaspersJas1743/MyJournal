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
		Activity.Statuses? activity = value as Activity.Statuses?;
		if (activity is null)
			return 0;

		return System.Convert.ToDouble(value: activity == Activity.Statuses.Online);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);
}