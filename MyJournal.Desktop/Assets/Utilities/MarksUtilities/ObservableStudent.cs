using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using DynamicData.Binding;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class ObservableStudent : ReactiveObject
{
	private readonly INotificationService _notificationService;
	private readonly StudentInTaughtClass? _studentInTaughtClass;
	private readonly StudentOfSubjectInClass? _studentOfSubjectInClass;
	private DateTimeOffset _dateTime;
	private PossibleAssessment? _selectedAssessment;
	private CommentsForAssessment? _selectedComment;
	private CommentsForAssessment _selectedAttendanceComment;
	private ObservableEstimationOfStudent? _selectedEstimation;
	private GradeTypes? _previousGradeType = null;
	private bool _isCreating = false;
	private bool _isEditing = false;
	private bool _isAttend = false;

	private ObservableStudent(
		int position,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	)
	{
		PossibleAssessments.Load(items: possibleAssessments);
		Position = position;
		Date = DateTimeOffset.Now;
		_notificationService = notificationService;

		CreateEstimation = ReactiveCommand.CreateFromTask<ListBoxItem>(execute: AddNewGrade);
		OnPossibleAssessmentSelectionChanged = ReactiveCommand.CreateFromTask(execute: PossibleAssessmentSelectionChangedHandler);
		OnDetachedFromVisualTree = ReactiveCommand.Create(execute: DetachedFromVisualTreeHandler);
		OnEstimationSelectionChanged = ReactiveCommand.CreateFromTask<ListBoxItem>(execute: EstimationSelectionChangedHandler);
		OnPointerPressed = ReactiveCommand.Create(execute: PointerPressedHandler);
		IObservable<bool> canSaveGrade = this.WhenAnyValue(
			property1: model => model.SelectedAssessment,
			property2: model => model.SelectedComment,
			selector: (assessment, comment) => assessment is not null && comment is not null
		);

		SaveNewEstimation = ReactiveCommand.CreateFromTask(execute: SaveNewGradeHandler, canExecute: canSaveGrade);
		SaveEditableEstimation = ReactiveCommand.CreateFromTask(execute: SaveEditableGradeHandler, canExecute: canSaveGrade);

		this.WhenAnyValue(property1: s => s.IsAttend).WhereNotNull().Subscribe(onNext: async isAttend =>
		{
			if (TruancyComments.Count != 0)
				return;

			PossibleAssessment truancy = PossibleAssessments.First(predicate: a => a.GradeType == GradeTypes.Truancy);
			TruancyComments.Load(items: await truancy.GetComments());
			if (!isAttend)
				SelectedAttendanceComment = TruancyComments[index: 0];
		});
	}

	public ObservableStudent(
		StudentInTaughtClass studentInTaughtClass,
		int position,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	) : this(
		position: position,
		possibleAssessments: possibleAssessments,
		notificationService: notificationService
	) => _studentInTaughtClass = studentInTaughtClass;

	public ObservableStudent(
		StudentOfSubjectInClass studentOfSubjectInClass,
		int position,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	) : this(
		position: position,
		possibleAssessments: possibleAssessments,
		notificationService: notificationService
	) => _studentOfSubjectInClass = studentOfSubjectInClass;

	public int Id => _studentInTaughtClass?.Id ?? _studentOfSubjectInClass!.Id;

	public string? Surname => _studentInTaughtClass?.Surname ?? _studentOfSubjectInClass?.Surname;

	public string? Name => _studentInTaughtClass?.Name ?? _studentOfSubjectInClass?.Name;

	public string? Patronymic => _studentInTaughtClass?.Patronymic ?? _studentOfSubjectInClass?.Patronymic;

	public int Position { get; }

	public bool IsAttend
	{
		get => _isAttend;
		set => this.RaiseAndSetIfChanged(backingField: ref _isAttend, newValue: value);
	}

    public ObservableGradeOfStudent? Grade { get; private set; }

	public ObservableCollectionExtended<PossibleAssessment> PossibleAssessments { get; }
		= new ObservableCollectionExtended<PossibleAssessment>();

	public ObservableCollectionExtended<CommentsForAssessment> Comments { get; }
		= new ObservableCollectionExtended<CommentsForAssessment>();

	public ObservableCollectionExtended<CommentsForAssessment> TruancyComments { get; }
		= new ObservableCollectionExtended<CommentsForAssessment>();

	public ObservableEstimationOfStudent? SelectedEstimation
	{
		get => _selectedEstimation;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedEstimation, newValue: value);
	}

	public PossibleAssessment? SelectedAssessment
	{
		get => _selectedAssessment;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedAssessment, newValue: value);
	}

	public CommentsForAssessment SelectedAttendanceComment
	{
		get => _selectedAttendanceComment;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedAttendanceComment, newValue: value);
	}

	public CommentsForAssessment? SelectedComment
	{
		get => _selectedComment;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedComment, newValue: value);
	}

	public DateTimeOffset Date
	{
		get => _dateTime;
		set => this.RaiseAndSetIfChanged(backingField: ref _dateTime, newValue: value);
	}

	public bool IsEditing
	{
		get => _isEditing;
		set => this.RaiseAndSetIfChanged(backingField: ref _isEditing, newValue: value);
	}

	public bool IsCreating
	{
		get => _isCreating;
		set => this.RaiseAndSetIfChanged(backingField: ref _isCreating, newValue: value);
	}

	public ReactiveCommand<ListBoxItem, Unit> CreateEstimation { get; }
	public ReactiveCommand<ListBoxItem, Unit> OnEstimationSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnPointerPressed { get; }
	public ReactiveCommand<Unit, Unit> OnPossibleAssessmentSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnDetachedFromVisualTree { get; }
	public ReactiveCommand<Unit, Unit> SaveNewEstimation { get; }
	public ReactiveCommand<Unit, Unit> SaveEditableEstimation { get; }

	private async Task AddNewGrade(ListBoxItem item)
	{
		item.IsSelected = !item.IsSelected;
		IsCreating = true;
	}

	public async Task LoadGrade()
	{
		Grade = _studentInTaughtClass is not null
			? (await _studentInTaughtClass.GetGrade()).ToObservable(possibleAssessments: PossibleAssessments, notificationService: _notificationService)
			: (await _studentOfSubjectInClass!.GetGrade()).ToObservable(possibleAssessments: PossibleAssessments, notificationService: _notificationService);

		await Grade.LoadEstimations();
	}

	public async Task LoadAttendance(DateTimeOffset dateTimeOffset)
	{
		if (Grade is null)
			return;

		DateOnly date = DateOnly.FromDateTime(dateTime: dateTimeOffset.Date);
		Debug.WriteLine($"date: {date}");
		IsAttend = Grade.Estimations.FirstOrDefault(predicate: e =>
			DateOnly.FromDateTime(dateTime: e.CreatedAt) == date &&
			e.GradeType == GradeTypes.Truancy
		) is null;
		Debug.WriteLine($"{date}: {Surname} {Name} {Patronymic} {(IsAttend ? "присутствует" : "отсутствует")}");
	}

	private async Task PossibleAssessmentSelectionChangedHandler()
	{
		if (SelectedAssessment is null || _previousGradeType == SelectedAssessment.GradeType)
			return;

		Comments.Load(items: await SelectedAssessment.GetComments());
		_previousGradeType = SelectedAssessment.GradeType;
	}

	private async Task SaveNewGradeHandler()
	{
		await Grade!.Add()
			.WithGrade(gradeId: SelectedAssessment!.Id)
			.WithComment(commentId: SelectedComment!.Id)
			.WithCreationDate(creationDate: Date.DateTime)
			.Save();

		await _notificationService.Show(
			title: "Успеваемость",
			content: "Отметка успешно добавлена!",
			type: NotificationType.Success
		);
		_previousGradeType = null;
	}

	private async Task SaveEditableGradeHandler()
	{
		await SelectedEstimation!.Change()
			.GradeTo(newGradeId: SelectedAssessment!.Id)
			.CommentTo(newCommentId: SelectedComment!.Id)
			.DatetimeTo(newDateTime: Date.DateTime)
			.Save();

		await _notificationService.Show(
			title: "Успеваемость",
			content: "Отметка успешно изменена!",
			type: NotificationType.Success
		);
		_previousGradeType = null;
	}

	private async Task EstimationSelectionChangedHandler(ListBoxItem item)
	{
		if (SelectedEstimation is null)
		{
			item.IsSelected = IsEditing = false;
			SelectedAssessment = null;
			_previousGradeType = null;
			Comments.Clear();
			return;
		}
		item.IsSelected = IsEditing = true;

		Date = new DateTimeOffset(dateTime: SelectedEstimation!.CreatedAt);
		PossibleAssessment assessment = PossibleAssessments.First(predicate: a => a.Assessment == SelectedEstimation!.Assessment);
		_previousGradeType = assessment.GradeType;
		SelectedAssessment = assessment;
		Comments.Load(items: await SelectedAssessment.GetComments());
		SelectedComment = Comments.First(predicate: c => c.Comment == SelectedEstimation.Comment);
	}

	private void PointerPressedHandler()
	{
		Date = DateTimeOffset.Now;
		if (IsEditing)
			return;

		IsCreating = true;
	}

	private void DetachedFromVisualTreeHandler()
	{
		Comments.Clear();
		SelectedAssessment = null;
		_previousGradeType = null;
	}
}

public static class ObservableStudentExtensions
{
	public static ObservableStudent ToObservable(
		this StudentInTaughtClass studentInTaughtClass,
		int position,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	)
	{
		return new ObservableStudent(
			studentInTaughtClass: studentInTaughtClass,
			position: position,
			possibleAssessments: possibleAssessments,
			notificationService: notificationService
		);
	}

	public static ObservableStudent ToObservable(
		this StudentOfSubjectInClass studentOfSubjectInClass,
		int position,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	)
	{
		return new ObservableStudent(
			studentOfSubjectInClass: studentOfSubjectInClass,
			position: position,
			possibleAssessments: possibleAssessments,
			notificationService: notificationService
		);
	}
}