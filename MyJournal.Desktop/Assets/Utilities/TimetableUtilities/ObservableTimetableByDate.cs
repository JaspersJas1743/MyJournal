using MyJournal.Core.SubEntities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class ObservableTimetableByDate : ReactiveObject
{
	private readonly TimetableForStudent? _timetableForStudent;
	private readonly TimetableForTeacher? _timetableForTeacher;

	public ObservableTimetableByDate(TimetableForStudent timetableForStudent)
		=> _timetableForStudent = timetableForStudent;

	public ObservableTimetableByDate(TimetableForTeacher timetableForTeacher)
		=> _timetableForTeacher = timetableForTeacher;

	public SubjectOnTimetable Subject => _timetableForStudent?.Subject ?? _timetableForTeacher!.Subject;
	public BreakAfterSubject? Break => _timetableForStudent?.Break ?? _timetableForTeacher!.Break;
	public bool IsTeacher => _timetableForTeacher is not null;
}

public static class ObservableTimetableByDateExtensions
{
	public static ObservableTimetableByDate ToObservable(this TimetableForStudent timetableForStudent)
		=> new ObservableTimetableByDate(timetableForStudent: timetableForStudent);

	public static ObservableTimetableByDate ToObservable(this TimetableForTeacher timetableForTeacher)
		=> new ObservableTimetableByDate(timetableForTeacher: timetableForTeacher);
}