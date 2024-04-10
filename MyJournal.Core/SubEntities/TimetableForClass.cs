namespace MyJournal.Core.SubEntities;

public sealed class TimetableForClass
{
	private TimetableForClass(
		DayOfWeek dayOfWeek,
		int totalHours,
		IEnumerable<SubjectInClassOnTimetable> subjects
	)
	{
		DayOfWeek = dayOfWeek;
		TotalHours = totalHours;
		Subjects = subjects;
	}

	public DayOfWeek DayOfWeek { get; set; }
	public int TotalHours { get; set; }
	public IEnumerable<SubjectInClassOnTimetable> Subjects { get; set; }

	internal static async Task<TimetableForClass> Create(
		DayOfWeek dayOfWeek,
		int totalHours,
		IEnumerable<SubjectInClassOnTimetable> subjects
	) => new TimetableForClass(dayOfWeek: dayOfWeek, totalHours: totalHours, subjects: subjects);
}