using System;
using Avalonia.Data.Converters;
using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class DateTimeConverters
{
	public static readonly IValueConverter IsCurrentEducationPeriod = new FuncValueConverter<EducationPeriod, bool>(convert: period =>
	{
		if (period is null)
			return false;

		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		return now >= period.StartDate && now <= period.EndDate;
	});
}