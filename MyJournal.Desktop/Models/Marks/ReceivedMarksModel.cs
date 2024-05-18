using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.MarksUtilities;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Marks;

public sealed class ReceivedMarksModel : MarksModel
{
	private readonly INotificationService _notificationService;
	private readonly ReadOnlyObservableCollection<StudentSubject> _studyingSubjects;
	private readonly SourceCache<StudentSubject, int> _studyingSubjectsCache = new SourceCache<StudentSubject, int>(keySelector: s => s.Id);
	private ObservableGrade? _grade;
	private StudentSubjectCollection _studentSubjectCollection;
	private string? _filter = String.Empty;
	private EducationPeriod? _selectedPeriod = null;

	public ReceivedMarksModel(INotificationService notificationService)
	{
		_notificationService = notificationService;
		OnTaskCompletionStatusSelectionChanged = ReactiveCommand.CreateFromTask(execute: TaskCompletionStatusSelectionChangedHandler);
		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		ClearTasks = ReactiveCommand.Create(execute: ClearSelection);

		IObservable<Func<StudentSubject, bool>> filter = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<StudentSubject>> sort = this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
		.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
		.Select(selector: _ => SortExpressionComparer<StudentSubject>.Ascending(expression: s => s.Teacher != null ? 1 : 0)
			.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.Teacher?.FullName ?? String.Empty));

		_ = _studyingSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
								  .Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();
	}

	private void ClearSelection()
	{
		SubjectSelectionModel.SelectedItem = null;
		Estimations.Clear();
		Grade = null;
	}

	private async Task SubjectSelectionChangedHandler()
	{
		if (SelectedPeriod is null || SubjectSelectionModel.SelectedItem is null)
			return;

		Grade<Estimation> grade = await SubjectSelectionModel.SelectedItem.GetGrade();
		Grade = grade.ToObservable();
		await Grade.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id);
		Estimations.Load(items: await Grade.GetEstimations());
	}

	private async Task TaskCompletionStatusSelectionChangedHandler()
	{
		if (SelectedPeriod is null || SubjectSelectionModel.SelectedItem is null)
			return;

		Grade<Estimation> grade = await SubjectSelectionModel.SelectedItem.GetGrade();
		Grade = grade.ToObservable();
		await Grade.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id);
		Estimations.Load(items: await Grade.GetEstimations());
	}

	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public ReadOnlyObservableCollection<StudentSubject> StudyingSubjects => _studyingSubjects;

	public SelectionModel<StudentSubject> SubjectSelectionModel { get; } = new SelectionModel<StudentSubject>();

	public ObservableCollectionExtended<EducationPeriod> EducationPeriods { get; }
		= new ObservableCollectionExtended<EducationPeriod>();

	public ObservableCollectionExtended<ObservableEstimation> Estimations { get; }
		= new ObservableCollectionExtended<ObservableEstimation>();

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

	public ObservableGrade? Grade
	{
		get => _grade;
		private set => this.RaiseAndSetIfChanged(backingField: ref _grade, newValue: value);
	}

	public Func<StudentSubject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.Teacher?.FullName.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	public override async Task SetUser(User user)
	{
		Parent? parent = user as Parent;

		_studentSubjectCollection = user is Student student
			? new StudentSubjectCollection(studyingSubjectCollection: await student.GetStudyingSubjects())
			: new StudentSubjectCollection(wardStudyingSubjectCollection: await parent!.GetWardSubjectsStudying());

		List<StudentSubject> subjects = await _studentSubjectCollection.ToListAsync();
		_studyingSubjectsCache.Edit(updateAction: (a) => a.AddOrUpdate(items: subjects.Skip(count: 1)));

		EducationPeriods.Load(items: await _studentSubjectCollection.GetEducationPeriods());
		SelectedPeriod = EducationPeriods[index: 0];

		_studentSubjectCollection.CreatedAssessment += OnCreatedAssessment;
		_studentSubjectCollection.CreatedFinalAssessment += OnCreatedFinalAssessment;
	}

	private async void OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		StudentSubject subject = await _studentSubjectCollection.FindById(id: e.SubjectId);
		await _notificationService.Show(
			title: "Новая отметка",
			content: $"По предмету \"{subject.Name}\" выставлена итоговая отметка"
		);
	}

	private async void OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		StudentSubject subject = await _studentSubjectCollection.FindById(id: e.SubjectId);
		await _notificationService.Show(
			title: "Новая отметка",
			content: $"Получена новая оценка по предмету \"{subject.Name}\""
		);

		if (SubjectSelectionModel.SelectedItem?.Id != e.SubjectId)
			return;

		IEnumerable<ObservableEstimation> estimations = await Grade!.GetEstimations();
		ObservableEstimation estimation = estimations.First(predicate: estimation => estimation.Id == e.AssessmentId);

		int index = Math.Max(val1: estimations.IndexOf(item: estimation), val2: 0);
		Estimations.Insert(item: estimation, index: index);
	}
}