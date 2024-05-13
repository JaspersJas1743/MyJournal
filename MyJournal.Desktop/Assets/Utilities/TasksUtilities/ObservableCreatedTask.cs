using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Timers;
using Humanizer;
using Humanizer.Localisation;
using MyJournal.Core.Builders.TaskBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ObservableCreatedTask : ReactiveObject
{
	private readonly Func<ITaskBuilder> _taskBuilderFactory;
	private ITaskBuilder? _taskBuilder;
	private readonly Dictionary<Class, IEnumerable<Subject>> _classSubjects = new Dictionary<Class, IEnumerable<Subject>>();
	private IEnumerable<Class>? _classes;
	private IEnumerable<Subject>? _subjects;
	private Class? _selectedClass;
	private Subject? _selectedSubject;
	private string _enteredText = String.Empty;
	private DateTimeOffset _selectedDate = new DateTimeOffset(dateTime: DateTime.Now);
	private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;

	private readonly CreatedTask? _taskToObservable;
	private List<ExtendedAttachment>? _attachments;
	private bool? _showClassAndLesson;
	private bool? _isExpired = false;
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 1));

	public ObservableCreatedTask(
		Dictionary<Class, IEnumerable<Subject>> classSubjects,
		ReactiveCommand<Unit, Unit> showAttachments
	)
	{
		_classSubjects = classSubjects;
		Classes = classSubjects.Keys.Skip(count: 1);
		ShowAttachments = showAttachments;

		MessageBus.Current.Listen<AddAttachmentToTaskEventArgs>().Subscribe(onNext: async e =>
		{
			await _taskBuilder?.AddAttachment(pathToFile: e.PathToFile)!;
			MessageBus.Current.SendMessage(message: new AttachmentAddedToTaskEventArgs());
		});
		MessageBus.Current.Listen<RemoveAttachmentFromTaskEventArgs>().Subscribe(onNext: async e =>
		{
			await _taskBuilder?.RemoveAttachment(pathToFile: e.PathToFile)!;
			MessageBus.Current.SendMessage(message: new AttachmentRemovedToTaskEventArgs());
		});
		OnClassSelectionChanged = ReactiveCommand.Create(execute: ClassSelectionChangedHandler);
	}

	private void ClassSelectionChangedHandler()
	{
		Subjects = _classSubjects[key: SelectedClass!];
		this.RaisePropertyChanged(propertyName: nameof(SingleSubject));
	}

	public ObservableCreatedTask(
		Dictionary<Class, IEnumerable<Subject>> classSubjects,
		int? selectedClassId,
		int? selectedSubjectId,
		ReactiveCommand<Unit, Unit> showAttachments
	) : this(
		classSubjects: classSubjects,
		showAttachments: showAttachments
	)
	{
		SelectedClass = classSubjects.Skip(count: 1).Select(selector: p => p.Key).FirstOrDefault(predicate: c => c?.Id == selectedClassId);
		if (SelectedClass is null)
			return;

		Subjects = classSubjects[key: SelectedClass];
		SelectedSubject = classSubjects.SelectMany(selector: p => p.Value).DistinctBy(keySelector: s => s.Id)
			.FirstOrDefault(predicate: s => s.Id == selectedSubjectId);

		_taskBuilder = SelectedSubject?.Create();
	}

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

	public IEnumerable<Class>? Classes
	{
		get => _classes;
		set => this.RaiseAndSetIfChanged(backingField: ref _classes, newValue: value);
	}

	public IEnumerable<Subject>? Subjects
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

	public DateTimeOffset SelectedDate
	{
		get => _selectedDate;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedDate, newValue: value);
	}

	public TimeSpan SelectedTime
	{
		get => _selectedTime;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedTime, newValue: value);
	}

	public string? Lesson => _taskToObservable?.LessonName;
	public string? Class => _taskToObservable?.ClassName;
	public bool SingleClass => _classes?.Count() == 1;
	public bool SingleSubject => _subjects?.Count() == 1;
	public int? CountOfCompletedTask => _taskToObservable?.CountOfCompletedTask;
	public int? CountOfUncompletedTask => _taskToObservable?.CountOfUncompletedTask;
	public bool? IsExpired => (ReleasedAt - DateTime.Now)?.TotalSeconds <= 1;
	public ReactiveCommand<Unit, Unit>? ShowAttachments { get; }
	public ReactiveCommand<Unit, Unit>? OnAttachedToVisualTree { get; }
	public ReactiveCommand<Unit, Unit>? OnDetachedFromVisualTree { get; }
	public ReactiveCommand<Unit, Unit>? OnClassSelectionChanged { get; }

	private void RaiseCompletionCount()
	{
		this.RaisePropertyChanged(propertyName: nameof(CountOfCompletedTask));
		this.RaisePropertyChanged(propertyName: nameof(CountOfUncompletedTask));
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