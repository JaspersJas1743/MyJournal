using System;
using System.Linq;
using Avalonia.Data.Converters;
using MyJournal.Desktop.Assets.Utilities;
using DayOfWeek = MyJournal.Core.SubEntities.DayOfWeek;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class TimetableConverters
{
	public static readonly IMultiValueConverter Header = new FuncMultiValueConverter<object, string>(convert: objects =>
	{
		object?[] timetable = objects.ToArray();
		if (timetable is not ({ Length: 2 } and [DayOfWeek dayOfWeek, double totalHours]))
			return String.Empty;

		return dayOfWeek.Name + (totalHours > 0
			? " - " + WordFormulator.GetForm(count: totalHours, forms: new string[] { "часов", "час", "часа" })
			: String.Empty);
	});
}