using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class TimetableByDateCollection : ReactiveObject
{
	private readonly TimetableForStudentCollection? _timetableForStudentCollection = null;
	private readonly TimetableForTeacherCollection? _timetableForTeacherCollection = null;
	private readonly TimetableForWardCollection? _timetableForWardCollection = null;

	public TimetableByDateCollection(TimetableForStudentCollection timetableForStudentCollection)
		=> _timetableForStudentCollection = timetableForStudentCollection;

	public TimetableByDateCollection(TimetableForTeacherCollection timetableForTeacherCollection)
		=> _timetableForTeacherCollection = timetableForTeacherCollection;

	public TimetableByDateCollection(TimetableForWardCollection timetableForWardCollection)
		=> _timetableForWardCollection = timetableForWardCollection;

	public async Task<IEnumerable<TimetableByDate>> GetTimetable(DateOnly date)
	{
		if (_timetableForTeacherCollection is not null)
		{
			IEnumerable<TimetableForTeacher> timetableForTeacher = await _timetableForTeacherCollection.GetByDate(date: date);
			return timetableForTeacher.Select(selector: t => t.ToObservable());
		}

		IEnumerable<TimetableForStudent> timetableForStudents = _timetableForStudentCollection is not null
			? await _timetableForStudentCollection.GetByDate(date: date)
			: await _timetableForWardCollection!.GetByDate(date: date);
		return timetableForStudents.Select(selector: t => t.ToObservable());
	}
}

public static class ObservableTimetableByDateCollectionExtensions
{
	public static TimetableByDateCollection ToObservable(this TimetableForStudentCollection timetableForStudentCollection)
		=> new TimetableByDateCollection(timetableForStudentCollection: timetableForStudentCollection);

	public static TimetableByDateCollection ToObservable(this TimetableForTeacherCollection timetableForTeacherCollection)
		=> new TimetableByDateCollection(timetableForTeacherCollection: timetableForTeacherCollection);

	public static TimetableByDateCollection ToObservable(this TimetableForWardCollection timetableForWardCollection)
		=> new TimetableByDateCollection(timetableForWardCollection: timetableForWardCollection);
}