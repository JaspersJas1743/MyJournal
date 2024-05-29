using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData.Binding;
using MyJournal.Core.Builders.TimetableBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class Class : ReactiveObject
{
	private readonly ObservableCollectionExtended<CreatingTimetable> _timetable =
		new ObservableCollectionExtended<CreatingTimetable>();
	private readonly INotificationService _notificationService;
	private readonly Core.SubEntities.Class _class;
	private bool _timetableIsActual = false;
	private bool _timetableIsInitialized = false;

	public Class(
		INotificationService notificationService,
		Core.SubEntities.Class @class
	)
	{
		_notificationService = notificationService;
		_class = @class;
		_class.ChangedTimetable += OnChangedTimetable;
	}

	private void OnChangedTimetable(ChangedTimetableEventArgs e)
	{
		_timetableIsInitialized = false;
		_timetableIsActual = false;
	}

	public int Id => _class.Id;
	public string? Name => _class.Name;

	public bool GetHaveChange() => _timetableIsActual && _timetableIsInitialized && _timetable.Any(predicate: t => t.GetHaveChange());

	public ITimetableBuilder CreateTimetable()
		=> _class.CreateTimetable();

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
				notificationService: _notificationService,
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
		_timetableIsInitialized = true;
		_timetableIsActual = true;
		return _timetable;
	}
}