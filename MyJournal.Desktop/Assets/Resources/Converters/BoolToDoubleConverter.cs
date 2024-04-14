using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class BoolToDoubleConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not bool boolean)
			return new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);

		return System.Convert.ToDouble(value: boolean);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);
}