using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Humanizer;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class DateOnlyConverters
{
	public static readonly IValueConverter StringFormat = new FuncValueConverter<DateOnly, string>(
		convert: date => date.ToString(format: "d MMMM", provider: CultureInfo.CurrentUICulture)
	);

	public static readonly IValueConverter CurrentDateStringFormat = new FuncValueConverter<DateOnly, string>(
		convert: date => $"{date.ToString(format: "MMM").ApplyCase(casing: LetterCasing.Title)}{date:, d}"
	);

	public static readonly IValueConverter DayOfWeek = new FuncValueConverter<DateOnly, string>(convert: date =>
		date.DayOfWeek switch
		{
			System.DayOfWeek.Sunday     => "Суббота",
			System.DayOfWeek.Monday     => "Понедельник",
			System.DayOfWeek.Tuesday	=> "Вторник",
			System.DayOfWeek.Wednesday	=> "Среда",
			System.DayOfWeek.Thursday	=> "Четверг",
			System.DayOfWeek.Friday		=> "Пятница",
			System.DayOfWeek.Saturday	=> "Суббота"
		}
	);

	public static readonly IValueConverter IsWeekend = new FuncValueConverter<DateOnly, bool>(convert: DateIsWeekend);

	public static readonly IValueConverter IsWorkingDay = new FuncValueConverter<DateOnly, bool>(
		convert: date => !DateIsWeekend(date: date)
	);

	private static bool DateIsWeekend(DateOnly date)
		=> date.DayOfWeek is System.DayOfWeek.Sunday or System.DayOfWeek.Saturday;
}