using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData.Binding;
using MyJournal.Core.Builders.TimetableBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class Class : ReactiveObject
{
	private readonly ObservableCollectionExtended<CreatingTimetable> _timetable =
		new ObservableCollectionExtended<CreatingTimetable>();
	private readonly Core.SubEntities.Class _class;
	private bool _timetableIsActual = false;

	public Class(Core.SubEntities.Class @class)
	{
		_class = @class;
		_class.ChangedTimetable += OnChangedTimetable;
	}

	private void OnChangedTimetable(ChangedTimetableEventArgs e)
		=> _timetableIsActual = false;

	public int Id => _class.Id;
	public string? Name => _class.Name;

	public async Task<ITimetableBuilder> CreateTimetable()
		=> await _class.CreateTimetable();

	public async Task<ObservableCollectionExtended<CreatingTimetable>> GetTimetable()
	{
		if (_timetableIsActual)
			return _timetable;

		StudyingSubjectInClassCollection subjectInClassCollection = await _class.GetStudyingSubjects();
		StudyingSubjectInClass[] subjectInClass = await subjectInClassCollection.ToArrayAsync();
		IEnumerable<Subject> possibleSubjects = subjectInClass.Select(selector: s => new Subject(id: s.Id, name: s.Name)).Skip(count: 1);
		IEnumerable<TimetableForClass> timetable = await _class.GetTimetable();
		await Dispatcher.UIThread.InvokeAsync(callback: () =>
		{
			_timetable.Load(items: timetable.Select(selector: t => new CreatingTimetable(
				dayOfWeek: t.DayOfWeek,
				totalHours: t.TotalHours,
				possibleSubjects: possibleSubjects,
				classId: Id,
				subjects: t.Subjects.Select(selector: s => new SubjectOnTimetable(
					number: s.Subject.Number,
					start: s.Subject.Start,
					end: s.Subject.End,
					possibleSubjects: possibleSubjects,
					selectedSubjectId: s.Subject.Id,
					@break: s.Break?.CountMinutes,
					dayOfWeek: t.DayOfWeek,
					classId: Id
				))
			)));
		});
		_timetableIsActual = true;
		return _timetable;
	}
}