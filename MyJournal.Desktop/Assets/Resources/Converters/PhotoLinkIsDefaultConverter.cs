using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class PhotoLinkIsDefaultConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string link)
			return new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);

		return link.Contains(value: "defaults", comparisonType: StringComparison.CurrentCultureIgnoreCase);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);
}