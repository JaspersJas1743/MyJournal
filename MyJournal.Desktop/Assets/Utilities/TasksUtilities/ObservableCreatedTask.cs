using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls.Notifications;
using Humanizer;
using Humanizer.Localisation;
using MyJournal.Core.Builders.TaskBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ObservableCreatedTask : ReactiveObject, IValidatableViewModel
{
	private readonly INotificationService _notificationService;
	private readonly Func<ITaskBuilder?>? _taskBuilderFactory = null;
	private ITaskBuilder? _taskBuilder = null;
	private readonly List<Class>? _classes;
	private List<Subject>? _subjects;
	private Class? _selectedClass;
	private Subject? _selectedSubject;
	private string _enteredText = String.Empty;
	private DateTimeOffset? _selectedDate;
	private TimeSpan? _selectedTime;
	private int _attachmentsCount = 0;

	private readonly CreatedTask? _taskToObservable;
	private List<ExtendedAttachment>? _attachments;
	private bool? _showClassAndLesson;
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 1));

	public ObservableCreatedTask(
		INotificationService notificationService,
		List<Class> classes,
		Func<ITaskBuilder?> taskBuilderFactory,
		ReactiveCommand<Unit, Unit> showAttachments
	)
	{
		_notificationService = notificationService;

		_taskBuilderFactory = taskBuilderFactory;
		_taskBuilder = _taskBuilderFactory();
		Classes = classes;
		ShowAttachments = showAttachments;
		SelectedTime = DateTime.Now.TimeOfDay;
		SelectedDate = new DateTimeOffset(dateTime: DateTime.Now);

		OnClassSelectionChanged = ReactiveCommand.Create(execute: ClassSelectionChangedHandler);
		SaveTask = ReactiveCommand.CreateFromTask(
			execute: SaveCurrentTask,
			canExecute: ValidationContext.Valid
		);

		SetValidationRule();

		MessageBus.Current.Listen<AddAttachmentToTaskEventArgs>().Subscribe(onNext: async e =>
		{
			await _taskBuilder?.AddAttachment(pathToFile: e.PathToFile)!;
			AttachmentsCount += 1;
			MessageBus.Current.SendMessage(message: new AttachmentAddedToTaskEventArgs());
		});
		MessageBus.Current.Listen<RemoveAttachmentFromTaskEventArgs>().Subscribe(onNext: async e =>
		{
			await _taskBuilder?.RemoveAttachment(pathToFile: e.PathToFile)!;
			AttachmentsCount -= 1;
			MessageBus.Current.SendMessage(message: new AttachmentRemovedFromTaskEventArgs());
		});
	}

	private async Task SaveCurrentTask()
	{
		try
		{
			await _taskBuilder!.SetClass(classId: SelectedClass!.Id)
				.SetSubject(subjectId: SelectedSubject!.Id)
				.SetText(text: String.IsNullOrWhiteSpace(value: EnteredText) ? null : EnteredText)
				.SetDateOfRelease(dateOfRelease: SelectedDate!.Value.Date.Add(value: SelectedTime!.Value))
				.Save();
			await _notificationService.Show(title: "Успех", content: "Задание сохранено!", type: NotificationType.Success);
		} catch (Exception ex)
		{
			await _notificationService.Show(title: "Ошибка сохранения", content: ex.Message, type: NotificationType.Error);
			return;
		}
		_taskBuilder = _taskBuilderFactory?.Invoke();
		EnteredText = String.Empty;
		AttachmentsCount = 0;
		MessageBus.Current.SendMessage(message: new TaskSavedEventArgs());
	}

	private async void ClassSelectionChangedHandler()
	{
		Subjects = await _classes?.Find(match: c => c.Id == SelectedClass!.Id)?.GetSubjects()!;
		this.RaisePropertyChanged(propertyName: nameof(SingleSubject));
	}

	public async Task SetSelectedClass(Class? selectedClass)
	{
		SelectedClass = selectedClass;
		Subjects = selectedClass is not null ? await selectedClass.GetSubjects() : null;
	}

	public async Task SetSelectedSubject(int selectedSubjectId)
		=> SelectedSubject = Subjects?.FirstOrDefault(predicate: s => s.Id == selectedSubjectId);

	public ObservableCreatedTask(
		CreatedTask task,
		bool showClassAndLesson,
		ReactiveCommand<Unit, Unit> showAttachments
	)
	{
		_taskToObservable = task;
		_timer.Elapsed += OnTimerElapsed;
		_timer.Start();

		ShowClassAndLesson = showClassAndLesson;
		ShowAttachments = showAttachments;
		OnAttachedToVisualTree = ReactiveCommand.Create(execute: () => _timer.Start());
		OnDetachedFromVisualTree = ReactiveCommand.Create(execute: () => _timer.Stop());

		_taskToObservable.Completed += _ => RaiseCompletionCount();
		_taskToObservable.Uncompleted += _ => RaiseCompletionCount();
	}

	~ObservableCreatedTask() => _timer.Dispose();

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
	{
		this.RaisePropertyChanged(propertyName: nameof(ReleasedTime));
		this.RaisePropertyChanged(propertyName: nameof(IsExpired));
	}

	public CreatedTask? Observable => _taskToObservable;
	public int? Id => _taskToObservable?.Id;
	public DateTime? ReleasedAt => _taskToObservable?.ReleasedAt;
	public string? ReleasedTime => (_taskToObservable?.ReleasedAt - DateTime.Now)?.Humanize(maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)
		.Replace(oldValue: "один", newValue: "1").Replace(oldValue: "одна", newValue: "1");
	public string? Text => _taskToObservable?.Content.Text;

	public int AttachmentsCount
	{
		get => _attachmentsCount;
		set => this.RaiseAndSetIfChanged(backingField: ref _attachmentsCount, newValue: value);
	}

	public List<Class>? Classes
	{
		get => _classes;
		init => this.RaiseAndSetIfChanged(backingField: ref _classes, newValue: value);
	}

	public List<Subject>? Subjects
	{
		get => _subjects;
		set => this.RaiseAndSetIfChanged(backingField: ref _subjects, newValue: value);
	}

	public List<ExtendedAttachment>? Attachments
	{
		get => _attachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _attachments, newValue: value);
	}

	public bool? ShowClassAndLesson
	{
		get => _showClassAndLesson;
		set => this.RaiseAndSetIfChanged(backingField: ref _showClassAndLesson, newValue: value);
	}

	public Class? SelectedClass
	{
		get => _selectedClass;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedClass, newValue: value);
	}

	public Subject? SelectedSubject
	{
		get => _selectedSubject;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedSubject, newValue: value);
	}

	public string EnteredText
	{
		get => _enteredText;
		set => this.RaiseAndSetIfChanged(backingField: ref _enteredText, newValue: value);
	}

	public DateTimeOffset? SelectedDate
	{
		get => _selectedDate;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedDate, newValue: value);
	}

	public TimeSpan? SelectedTime
	{
		get => _selectedTime;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedTime, newValue: value);
	}

	public string? Lesson => _taskToObservable?.LessonName;
	public string? Class => _taskToObservable?.ClassName;
	public bool SingleClass => _classes?.Count == 1;
	public bool SingleSubject => _subjects?.Count == 1;
	public int? CountOfCompletedTask => _taskToObservable?.CountOfCompletedTask;
	public int? CountOfUncompletedTask => _taskToObservable?.CountOfUncompletedTask;
	public bool? IsExpired => (ReleasedAt - DateTime.Now)?.TotalSeconds <= 1;
	public ReactiveCommand<Unit, Unit>? ShowAttachments { get; }
	public ReactiveCommand<Unit, Unit>? OnAttachedToVisualTree { get; }
	public ReactiveCommand<Unit, Unit>? OnDetachedFromVisualTree { get; }
	public ReactiveCommand<Unit, Unit>? OnClassSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit>? SaveTask { get; }

	private void RaiseCompletionCount()
	{
		this.RaisePropertyChanged(propertyName: nameof(CountOfCompletedTask));
		this.RaisePropertyChanged(propertyName: nameof(CountOfUncompletedTask));
	}

	public ValidationContext ValidationContext { get; } = new ValidationContext();

	private void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: task => task.SelectedClass,
			isPropertyValid: @class => @class is not null,
			message: "Необходимо указать класс."
		);

		this.ValidationRule(
			viewModelProperty: task => task.SelectedSubject,
			isPropertyValid: subject => subject is not null,
			message: "Необходимо указать класс."
		);

		IObservable<bool> emptyContentObservable = this.WhenAnyValue(
			property1: model => model.EnteredText,
			property2: model => model.AttachmentsCount,
			selector: (text, attachmentsCount) =>
				!String.IsNullOrEmpty(value: text) || attachmentsCount > 0
		);

		this.ValidationRule(validationObservable: emptyContentObservable, message: "Содержимое задачи отсутствует.");
	}
}

public static class ObservableCreatedTaskExtensions
{
	public static ObservableCreatedTask ToObservable(this CreatedTask task, bool showLessonName, ReactiveCommand<Unit, Unit> showAttachments)
	{
		ObservableCreatedTask observableTask = new ObservableCreatedTask(task: task, showClassAndLesson: showLessonName, showAttachments: showAttachments);
		observableTask.Attachments = new List<ExtendedAttachment>(collection: task.Content.Attachments?.Select(
			selector: a => a.ToExtended()
		) ?? Enumerable.Empty<ExtendedAttachment>());
		return observableTask;
	}

	public static ObservableCreatedTask ToObservable(this MyJournal.Core.SubEntities.CreatedTask task, bool showLessonName, ReactiveCommand<Unit, Unit> showAttachments)
		=> new CreatedTask(createdTask: task).ToObservable(showLessonName: showLessonName, showAttachments: showAttachments);

	public static ObservableCreatedTask ToObservable(this TaskAssignedToClass task, bool showLessonName, ReactiveCommand<Unit, Unit> showAttachments)
		=> new CreatedTask(taskAssignedToClass: task).ToObservable(showLessonName: showLessonName, showAttachments: showAttachments);
}