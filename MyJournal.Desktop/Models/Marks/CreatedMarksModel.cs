using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
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
	private DateTimeOffset _selectedDateForAttendance = DateTimeOffset.Now;
	private EducationPeriod? _selectedPeriod = null;
	private bool _attendanceIsChecking = false;
	private bool _finalGradesIsCreating = false;
	private DateTimeOffset? _attendanceLoadedAt = null;
	private bool _loaded = false;

	public CreatedMarksModel(
		INotificationService notificationService
	)
	{
		_notificationService = notificationService;

		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		OnEducationPeriodSelectionChanged = ReactiveCommand.CreateFromTask(execute: EducationPeriodSelectionChangedHandler);
		ClearTasks = ReactiveCommand.Create(execute: ClearSelection);
		ToAttendance = ReactiveCommand.CreateFromTask(execute: ToAttendanceChecking);
		SaveAttendance = ReactiveCommand.CreateFromTask(execute: SaveAttendanceChecking);
		ToFinalAssessments = ReactiveCommand.CreateFromTask(execute: ToFinalAssessmentsCreating);
		ToGrade = ReactiveCommand.CreateFromTask(execute: MoveToGrade);

		IObservable<Func<TeacherSubject, bool>> filter = this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<TeacherSubject>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<TeacherSubject>.Ascending(expression: s => s.ClassName != null ? 1 : 0)
				.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.ClassName ?? String.Empty));

		_ = _teacherSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
			.Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();

		this.WhenAnyValue(property1: model => model.SelectedDateForAttendance)
			.WhereNotNull().Where(predicate: _ => _attendanceLoadedAt is not null)
			.Subscribe(onNext: SelectedDateForAttendanceHandler);
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

	public DateTimeOffset SelectedDateForAttendance
	{
		get => _selectedDateForAttendance;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedDateForAttendance, newValue: value);
	}

	public bool AttendanceIsChecking
	{
		get => _attendanceIsChecking;
		set => this.RaiseAndSetIfChanged(backingField: ref _attendanceIsChecking, newValue: value);
	}

	public bool FinalGradesIsCreating
	{
		get => _finalGradesIsCreating;
		set => this.RaiseAndSetIfChanged(backingField: ref _finalGradesIsCreating, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnEducationPeriodSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }
	public ReactiveCommand<Unit, Unit> ToAttendance { get; }
	public ReactiveCommand<Unit, Unit> SaveAttendance { get; }
	public ReactiveCommand<Unit, Unit> ToFinalAssessments { get; }
	public ReactiveCommand<Unit, Unit> ToGrade { get; }

	private async void SelectedDateForAttendanceHandler(DateTimeOffset _)
	{
		if (_attendanceLoadedAt != SelectedDateForAttendance)
			await LoadAttendanceForStudents();

		_attendanceLoadedAt = SelectedDateForAttendance;
	}

	public Func<TeacherSubject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.ClassName?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	private async Task SubjectSelectionChangedHandler()
	{
		if (SubjectSelectionModel.SelectedItem?.ClassId is null)
			return;

		if (_loaded)
			return;
		_loaded = true;

		int classId = SubjectSelectionModel.SelectedItem.ClassId.Value;
		IEnumerable<EducationPeriod> periods = await _teacherSubjectCollection.GetEducationPeriods(classId: classId);
		EducationPeriods.Load(items: periods);
		SelectedPeriod = EducationPeriods[index: 0];

		await SubjectSelectionModel.SelectedItem.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id);
		IEnumerable<ObservableStudent> students = await SubjectSelectionModel.SelectedItem.GetClass();
		Students.Load(items: students);
		_loaded = false;
	}

	private async Task EducationPeriodSelectionChangedHandler()
	{
		if (SelectedPeriod is null || SubjectSelectionModel.SelectedItem?.ClassId is null)
			return;

		if (_loaded)
			return;
		_loaded = true;

		await SubjectSelectionModel.SelectedItem.SetEducationPeriod(educationPeriodId: SelectedPeriod.Id);
		IEnumerable<ObservableStudent> students = await SubjectSelectionModel.SelectedItem.GetClass();
		Students.Load(items: students);
		_loaded = false;
	}

	private void ClearSelection()
	{
		SubjectSelectionModel.SelectedItem = null;
		EducationPeriods.Clear();
		Students.Clear();
		FinalGradesIsCreating = false;
		AttendanceIsChecking = false;
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
		_teacherSubjectsCache.Edit(updateAction: (a) => a.AddOrUpdate(items: subjects));
	}

	private async Task MoveToGrade()
	{
		AttendanceIsChecking = false;
		FinalGradesIsCreating = false;
	}

	private async Task SaveAttendanceChecking()
	{
		if (SubjectSelectionModel.SelectedItem is null)
			return;

		await SubjectSelectionModel.SelectedItem.SetAttendance(
			date: SelectedDateForAttendance.Date,
			attendance: Students.Select(selector: s => new Attendance(
				StudentId: s.Id,
				IsAttend: s.IsAttend,
				CommentId: s.IsAttend ? null : s.SelectedAttendanceComment?.Id
			))
		);
		AttendanceIsChecking = false;

		await _notificationService.Show(
			title: "Посещаемость",
			content: $"Посещаемость на {SelectedDateForAttendance.Date.ToString(format: "d MMMM", provider: CultureInfo.CurrentUICulture)} сохранена!",
			type: NotificationType.Success
		);
	}

	private async Task ToFinalAssessmentsCreating()
	{
		FinalGradesIsCreating = true;
	}

	private async Task ToAttendanceChecking()
	{
		AttendanceIsChecking = true;
		await LoadAttendanceForStudents();
	}

	private async Task LoadAttendanceForStudents()
	{
		foreach (ObservableStudent observableStudent in Students.AsEnumerable())
			await observableStudent.LoadAttendance(dateTimeOffset: SelectedDateForAttendance);
	}
}