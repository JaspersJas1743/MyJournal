using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Timers;
using Humanizer;
using Humanizer.Localisation;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ObservableCreatedTask : ReactiveObject
{
	private readonly CreatedTask _taskToObservable;
	private List<ExtendedAttachment>? _attachments;
	private bool _showClassAndLesson;
	private bool _isExpired = false;
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 1));

	public ObservableCreatedTask(CreatedTask task, bool showClassAndLesson, ReactiveCommand<Unit, Unit> showAttachments)
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

	public CreatedTask Observable => _taskToObservable;
	public int Id => _taskToObservable.Id;
	public DateTime ReleasedAt => _taskToObservable.ReleasedAt;
	public string ReleasedTime => (_taskToObservable.ReleasedAt - DateTime.Now).Humanize(maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)
		.Replace(oldValue: "один", newValue: "1").Replace(oldValue: "одна", newValue: "1");
	public string? Text => _taskToObservable.Content.Text;
	public List<ExtendedAttachment>? Attachments
	{
		get => _attachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _attachments, newValue: value);
	}

	public bool ShowClassAndLesson
	{
		get => _showClassAndLesson;
		set => this.RaiseAndSetIfChanged(backingField: ref _showClassAndLesson, newValue: value);
	}

	public string Lesson => _taskToObservable.LessonName;
	public string Class => _taskToObservable.ClassName;
	public int CountOfCompletedTask => _taskToObservable.CountOfCompletedTask;
	public int CountOfUncompletedTask => _taskToObservable.CountOfUncompletedTask;
	public bool IsExpired => (ReleasedAt - DateTime.Now).TotalSeconds <= 1;
	public ReactiveCommand<Unit, Unit> ShowAttachments { get; }
	public ReactiveCommand<Unit, Unit> OnAttachedToVisualTree { get; }
	public ReactiveCommand<Unit, Unit> OnDetachedFromVisualTree { get; }

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