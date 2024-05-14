using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class EnumToDescriptionConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		Type? enumType = value?.GetType();
		if (enumType is null)
			return String.Empty;

		MemberInfo[] enumMember = enumType.GetMember(name: value?.ToString()!);
		MemberInfo? enumValueMemberInfo = enumMember.FirstOrDefault(m => m.DeclaringType == enumType);
		object[]? valueAttributes = enumValueMemberInfo?.GetCustomAttributes(typeof(DescriptionAttribute), inherit: false);
			return ((DescriptionAttribute)valueAttributes![0]).Description;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> new BindingNotification(error: new InvalidCastException(), errorType: BindingErrorType.Error);
}