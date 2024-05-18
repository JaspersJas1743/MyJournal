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
using ReactiveUI;

namespace MyJournal.Desktop.Models.Marks;

public sealed class ReceivedMarksModel : MarksModel
{
	private readonly ReadOnlyObservableCollection<StudentSubject> _studyingSubjects;
	private readonly SourceCache<StudentSubject, int> _studyingSubjectsCache = new SourceCache<StudentSubject, int>(keySelector: s => s.Id);
	private Grade<Estimation> _grade;
	private StudentSubjectCollection _studentSubjectCollection;
	private string? _filter = String.Empty;
	private string? _final = String.Empty;
	private string? _average = String.Empty;
	private EducationPeriod? _selectedPeriod = null;

	public ReceivedMarksModel()
	{
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
		Average = null;
		Final = null;
	}

	private async Task SubjectSelectionChangedHandler()
	{
		if (SelectedPeriod is null || SubjectSelectionModel.SelectedItem is null)
			return;

		_grade = await SubjectSelectionModel.SelectedItem.GetGrade();
		await _grade.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id);
		Final = _grade.FinalAssessment;
		Average = _grade.AverageAssessment;
		Estimations.Load(items: await _grade.GetEstimations());
	}

	private async Task TaskCompletionStatusSelectionChangedHandler()
	{
		if (SelectedPeriod is null || SubjectSelectionModel.SelectedItem is null)
			return;

		_grade = await SubjectSelectionModel.SelectedItem.GetGrade();
		await _grade.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id);
		Final = _grade.FinalAssessment;
		Average = _grade.AverageAssessment;
		Estimations.Load(items: await _grade.GetEstimations());
	}

	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public ReadOnlyObservableCollection<StudentSubject> StudyingSubjects => _studyingSubjects;

	public SelectionModel<StudentSubject> SubjectSelectionModel { get; } = new SelectionModel<StudentSubject>();

	public ObservableCollectionExtended<EducationPeriod> EducationPeriods { get; }
		= new ObservableCollectionExtended<EducationPeriod>();

	public ObservableCollectionExtended<Estimation> Estimations { get; }
		= new ObservableCollectionExtended<Estimation>();

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

	public string? Average
	{
		get => _average;
		set => this.RaiseAndSetIfChanged(backingField: ref _average, newValue: value);
	}

	public string? Final
	{
		get => _final;
		set => this.RaiseAndSetIfChanged(backingField: ref _final, newValue: value);
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

		_studentSubjectCollection.CreatedAssessment += OnStudyingSubjectCreatedAssessment;
		_studentSubjectCollection.CreatedFinalAssessment += OnStudyingSubjectCreatedFinalAssessment;
	}

	private void OnStudyingSubjectCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		throw new NotImplementedException();
	}

	private void OnStudyingSubjectCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		throw new NotImplementedException();
	}
}