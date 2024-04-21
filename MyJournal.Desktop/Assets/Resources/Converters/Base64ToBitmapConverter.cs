using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class Base64ToBitmapConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		Debug.WriteLine($"[Base64ToBitmapConverter] value.GetType(): {value.GetType()}");
		if (value is not string base64)
			return new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);

		byte[] bytes = System.Convert.FromBase64String(s: base64.Split(',')[1]);
		return new Bitmap(new MemoryStream(bytes));
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);

}