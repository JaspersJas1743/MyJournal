using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.MarksUtilities;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Marks;

public sealed class CreatedMarksModel : MarksModel
{
	private readonly INotificationService _notificationService;
	private readonly SourceCache<TeacherSubject, int> _teacherSubjectsCache =
		new SourceCache<TeacherSubject, int>(keySelector: s => s.Id);
	private readonly ReadOnlyObservableCollection<TeacherSubject> _studyingSubjects;
	private TeacherSubjectCollection _teacherSubjectCollection;
	private string? _filter = String.Empty;
	private EducationPeriod? _selectedPeriod = null;

	public CreatedMarksModel(
		INotificationService notificationService
	)
	{
		_notificationService = notificationService;

		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		OnTaskCompletionStatusSelectionChanged = ReactiveCommand.CreateFromTask(execute: TaskCompletionStatusSelectionChangedHandler);
		ClearTasks = ReactiveCommand.Create(execute: ClearSelection);

		IObservable<Func<TeacherSubject, bool>> filter = this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<TeacherSubject>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<TeacherSubject>.Ascending(expression: s => s.ClassName != null ? 1 : 0)
				.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.ClassName ?? String.Empty));

		_ = _teacherSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
			.Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();
	}

	public SelectionModel<TeacherSubject> SubjectSelectionModel { get; } = new SelectionModel<TeacherSubject>();

	public ObservableCollectionExtended<EducationPeriod> EducationPeriods { get; }
		= new ObservableCollectionExtended<EducationPeriod>();

	public ObservableCollectionExtended<ObservableStudent> Students { get; }
		= new ObservableCollectionExtended<ObservableStudent>();

	public ReadOnlyObservableCollection<TeacherSubject> StudyingSubjects => _studyingSubjects;

	public EducationPeriod? SelectedPeriod
	{
		get => _selectedPeriod;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedPeriod, newValue: value);
	}

	public string? Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public Func<TeacherSubject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.ClassName?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	private async Task SubjectSelectionChangedHandler()
	{
		if (SubjectSelectionModel.SelectedItem?.ClassId is null)
			return;

		int classId = SubjectSelectionModel.SelectedItem.ClassId.Value;
		IEnumerable<EducationPeriod> periods = await _teacherSubjectCollection.GetEducationPeriods(classId: classId);
		EducationPeriods.Load(items: periods);
		SelectedPeriod = EducationPeriods[index: 0];

		IEnumerable<ObservableStudent> students = await SubjectSelectionModel.SelectedItem.GetClass();
		Students.Load(items: students);
	}

	private async Task TaskCompletionStatusSelectionChangedHandler()
	{
		if (SelectedPeriod is null || SubjectSelectionModel.SelectedItem?.ClassId is null)
			return;

		IEnumerable<ObservableStudent> students = await SubjectSelectionModel.SelectedItem.GetClass();
		Students.Load(items: await Task.WhenAll(tasks: students.Select(selector: async s =>
		{
			await s.Grade?.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id)!;
			return s;
		})));
	}

	private void ClearSelection()
	{
		SubjectSelectionModel.SelectedItem = null;
		EducationPeriods.Clear();
		Students.Clear();
	}

	public override async Task SetUser(User user)
	{
		Administrator? administrator = user as Administrator;
		Teacher? teacher = user as Teacher;

		TaughtSubjectCollection? taughtSubjectCollection = teacher is not null ? await teacher.GetTaughtSubjects() : null;
		ClassCollection? classCollection = administrator is not null ? await administrator.GetClasses() : null;
		IEnumerable<PossibleAssessment> possibleAssessments = teacher is not null
			? await teacher.GetPossibleAssessments()
			: await administrator!.GetPossibleAssessments();
		_teacherSubjectCollection = taughtSubjectCollection is not null
			? new TeacherSubjectCollection(taughtSubjectCollection: taughtSubjectCollection, possibleAssessments: possibleAssessments)
            : new TeacherSubjectCollection(classCollection: classCollection!, possibleAssessments: possibleAssessments);

		List<TeacherSubject> subjects = await _teacherSubjectCollection.ToListAsync(notificationService: _notificationService);
			_teacherSubjectsCache.Edit(updateAction: (a) => a.AddOrUpdate(items: subjects.Skip(count: 1)));
	}
}