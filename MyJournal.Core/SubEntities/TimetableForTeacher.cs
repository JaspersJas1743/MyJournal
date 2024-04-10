namespace MyJournal.Core.SubEntities;

public sealed class TimetableForTeacher
{
	private TimetableForTeacher(
		SubjectOnTimetable subject,
		BreakAfterSubject? @break
	)
	{
		Subject = subject;
		Break = @break;
	}

	public SubjectOnTimetable Subject { get; set; }
	public BreakAfterSubject? Break { get; set; }

	internal static async Task<TimetableForTeacher> Create(
		SubjectOnTimetable subject,
		BreakAfterSubject? @break
	) => new TimetableForTeacher(subject: subject, @break: @break);
}