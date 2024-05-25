using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class Timetable
{
	private readonly TimetableForStudent? _timetableForStudent;
	private readonly TimetableForTeacher? _timetableForTeacher;

	public Timetable(TimetableForStudent timetableForStudent)
		=> _timetableForStudent = timetableForStudent;

	public Timetable(TimetableForTeacher timetableForTeacher)
		=> _timetableForTeacher = timetableForTeacher;

	public SubjectOnTimetable Subject => _timetableForStudent?.Subject ?? _timetableForTeacher!.Subject;
	public BreakAfterSubject? Break => _timetableForStudent is not null ? _timetableForStudent.Break : _timetableForTeacher!.Break;

	public bool IsTeacher => _timetableForTeacher is not null;
}

public static class ObservableTimetableByDateExtensions
{
	public static Timetable ToObservable(this TimetableForStudent timetableForStudent)
		=> new Timetable(timetableForStudent: timetableForStudent);

	public static Timetable ToObservable(this TimetableForTeacher timetableForTeacher)
		=> new Timetable(timetableForTeacher: timetableForTeacher);
}