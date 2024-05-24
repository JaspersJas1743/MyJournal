using MyJournal.Core.SubEntities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class TimetableByDate : ReactiveObject
{
	private readonly TimetableForStudent? _timetableForStudent;
	private readonly TimetableForTeacher? _timetableForTeacher;

	public TimetableByDate(TimetableForStudent timetableForStudent)
		=> _timetableForStudent = timetableForStudent;

	public TimetableByDate(TimetableForTeacher timetableForTeacher)
		=> _timetableForTeacher = timetableForTeacher;

	public SubjectOnTimetable Subject => _timetableForStudent?.Subject ?? _timetableForTeacher!.Subject;
	public BreakAfterSubject? Break => _timetableForStudent is not null ? _timetableForStudent.Break : _timetableForTeacher!.Break;

	public bool IsTeacher => _timetableForTeacher is not null;
}

public static class ObservableTimetableByDateExtensions
{
	public static TimetableByDate ToObservable(this TimetableForStudent timetableForStudent)
		=> new TimetableByDate(timetableForStudent: timetableForStudent);

	public static TimetableByDate ToObservable(this TimetableForTeacher timetableForTeacher)
		=> new TimetableByDate(timetableForTeacher: timetableForTeacher);
}