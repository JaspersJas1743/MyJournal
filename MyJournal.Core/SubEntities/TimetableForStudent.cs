namespace MyJournal.Core.SubEntities;

public class TimetableForStudent
{
	private TimetableForStudent(
		SubjectOnTimetable subject,
		IEnumerable<EstimationOnTimetable> estimations,
		BreakAfterSubject? @break
	)
	{
		Subject = subject;
		Estimations = estimations;
		Break = @break;
	}

	public SubjectOnTimetable Subject { get; set; }
	public IEnumerable<EstimationOnTimetable> Estimations { get; set; }
	public BreakAfterSubject? Break { get; set; }

	internal static TimetableForStudent Create(
		SubjectOnTimetable subject,
		IEnumerable<EstimationOnTimetable> estimations,
		BreakAfterSubject? @break
	) => new TimetableForStudent(subject: subject, estimations: estimations, @break: @break);
}