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

	public static readonly IValueConverter IsNotCurrentEducationPeriod = new FuncValueConverter<EducationPeriod, bool>(convert: period =>
	{
		if (period is null)
			return false;

		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		return !(now >= period.StartDate && now <= period.EndDate);
	});

	public static readonly IValueConverter CanSetFinalGrade = new FuncValueConverter<EducationPeriod, bool>(convert: period =>
	{
		if (period?.EndDate is null)
			return false;

		DateTime now = DateTime.Now;
		DateTime end = period.EndDate.Value.ToDateTime(time: TimeOnly.MinValue);
		return (end - now).TotalDays > 0;
	});

	public static readonly IValueConverter CanNotSetFinalGrade = new FuncValueConverter<EducationPeriod, bool>(convert: period =>
	{
		if (period?.EndDate is null)
			return false;

		DateTime now = DateTime.Now;
		DateTime end = period.EndDate.Value.ToDateTime(time: TimeOnly.MinValue);
		return (end - now).TotalDays <= 0;
	});

	public static readonly IValueConverter During = new FuncValueConverter<SubjectOnTimetable, string>(
		convert: subject => $"{subject?.Start:hh\\:mm} - {subject?.End:hh\\:mm}"
	);
}