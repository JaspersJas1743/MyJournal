using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class ObservableTimetableByDateCollection : ReactiveObject
{
	private readonly TimetableForStudentCollection? _timetableForStudentCollection = null;
	private readonly TimetableForTeacherCollection? _timetableForTeacherCollection = null;
	private readonly TimetableForWardCollection? _timetableForWardCollection = null;

	public ObservableTimetableByDateCollection(TimetableForStudentCollection timetableForStudentCollection)
		=> _timetableForStudentCollection = timetableForStudentCollection;

	public ObservableTimetableByDateCollection(TimetableForTeacherCollection timetableForTeacherCollection)
		=> _timetableForTeacherCollection = timetableForTeacherCollection;

	public ObservableTimetableByDateCollection(TimetableForWardCollection timetableForWardCollection)
		=> _timetableForWardCollection = timetableForWardCollection;

	public async Task<IEnumerable<ObservableTimetableByDate>> GetTimetable(DateOnly date)
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
	public static ObservableTimetableByDateCollection ToObservable(this TimetableForStudentCollection timetableForStudentCollection)
		=> new ObservableTimetableByDateCollection(timetableForStudentCollection: timetableForStudentCollection);

	public static ObservableTimetableByDateCollection ToObservable(this TimetableForTeacherCollection timetableForTeacherCollection)
		=> new ObservableTimetableByDateCollection(timetableForTeacherCollection: timetableForTeacherCollection);

	public static ObservableTimetableByDateCollection ToObservable(this TimetableForWardCollection timetableForWardCollection)
		=> new ObservableTimetableByDateCollection(timetableForWardCollection: timetableForWardCollection);
}