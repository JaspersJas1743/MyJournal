using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
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
	private ObservableEstimationOfStudent? _selectedEstimation;
	private bool _isCreating = false;
	private bool _isEditing = false;

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
		OnDetachedFromVisualTree = ReactiveCommand.Create(execute: () =>
		{
			Comments.Clear();
			SelectedAssessment = null;
		});
		OnEstimationSelectionChanged = ReactiveCommand.CreateFromTask<ListBoxItem>(execute: EstimationSelectionChangedHandler);
		OnPointerPressed = ReactiveCommand.Create(execute: () =>
		{
			Date = DateTimeOffset.Now;
			if (IsEditing)
				return;

			IsCreating = true;
		});
		IObservable<bool> saveGradeObservable = this.WhenAnyValue(
			property1: model => model.SelectedAssessment,
			property2: model => model.SelectedComment,
			selector: (assessment, comment) => assessment is not null && comment is not null
		);
		SaveNewEstimation = ReactiveCommand.CreateFromTask(execute: SaveNewGradeHandler, canExecute: saveGradeObservable);
		SaveEditableEstimation = ReactiveCommand.CreateFromTask(execute: SaveEditableGradeHandler, canExecute: saveGradeObservable);
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
	)
	{
		_studentInTaughtClass = studentInTaughtClass;

		_studentInTaughtClass.CreatedFinalAssessment += e => CreatedFinalAssessment?.Invoke(e: e);
		_studentInTaughtClass.CreatedAssessment += e => CreatedAssessment?.Invoke(e: e);
		_studentInTaughtClass.ChangedAssessment += e => ChangedAssessment?.Invoke(e: e);
		_studentInTaughtClass.DeletedAssessment += e => DeletedAssessment?.Invoke(e: e);
	}

	public ObservableStudent(
		StudentOfSubjectInClass studentOfSubjectInClass,
		int position,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	) : this(
		position: position,
		possibleAssessments: possibleAssessments,
		notificationService: notificationService
	)
	{
		_studentOfSubjectInClass = studentOfSubjectInClass;

		_studentOfSubjectInClass.CreatedFinalAssessment += e => CreatedFinalAssessment?.Invoke(e: e);
		_studentOfSubjectInClass.CreatedAssessment += e => CreatedAssessment?.Invoke(e: e);
		_studentOfSubjectInClass.ChangedAssessment += e => ChangedAssessment?.Invoke(e: e);
		_studentOfSubjectInClass.DeletedAssessment += e => DeletedAssessment?.Invoke(e: e);
	}

	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;

	public int Id => _studentInTaughtClass?.Id ?? _studentOfSubjectInClass!.Id;

	public string Surname => _studentInTaughtClass?.Surname ?? _studentOfSubjectInClass!.Surname;

	public string Name => _studentInTaughtClass?.Name ?? _studentOfSubjectInClass!.Name;

	public string? Patronymic => _studentInTaughtClass?.Patronymic ?? _studentOfSubjectInClass!.Patronymic;

	public int Position { get; }

    public ObservableGradeOfStudent? Grade { get; private set; }

	public ObservableCollectionExtended<PossibleAssessment> PossibleAssessments { get; }
		= new ObservableCollectionExtended<PossibleAssessment>();

	public ObservableCollectionExtended<CommentsForAssessment> Comments { get; }
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

	private async Task PossibleAssessmentSelectionChangedHandler()
	{
		if (SelectedAssessment is null)
			return;

		Comments.Load(items: await SelectedAssessment.GetComments());
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
	}

	private async Task EstimationSelectionChangedHandler(ListBoxItem item)
	{
		if (SelectedEstimation is null)
		{
			item.IsSelected = false;
			IsEditing = false;
			SelectedAssessment = null;
			Comments.Clear();
			return;
		}
		item.IsSelected = true;
		IsEditing = true;

		Date = new DateTimeOffset(dateTime: SelectedEstimation!.CreatedAt);
		SelectedAssessment = PossibleAssessments.First(predicate: a => a.Assessment == SelectedEstimation!.Assessment);
		Comments.Load(items: await SelectedAssessment.GetComments());
		SelectedComment = Comments.First(predicate: c => c.Comment == SelectedEstimation.Comment);
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