namespace MyJournal.Core.SubEntities;

public class TimetableForStudent
{
	private TimetableForStudent(
		SubjectOnTimetable subject,
		BreakAfterSubject? @break
	)
	{
		Subject = subject;
		Break = @break;
	}

	public SubjectOnTimetable Subject { get; set; }
	public BreakAfterSubject? Break { get; set; }

	internal static TimetableForStudent Create(
		SubjectOnTimetable subject,
		BreakAfterSubject? @break
	) => new TimetableForStudent(subject: subject, @break: @break);
}