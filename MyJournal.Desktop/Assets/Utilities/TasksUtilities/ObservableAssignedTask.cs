using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Timers;
using Humanizer;
using Humanizer.Localisation;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ObservableAssignedTask : ReactiveObject
{
	private readonly AssignedTask _taskToObservable;
	private List<ExtendedAttachment>? _attachments;
	private bool _showLessonName;
	private bool _isExpired = false;
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 1));

	public ObservableAssignedTask(AssignedTask task, bool showLessonName, ReactiveCommand<Unit, Unit> showAttachments)
	{
		_taskToObservable = task;
		_timer.Elapsed += OnTimerElapsed;
		_timer.Start();

		ShowLessonName = showLessonName;
		ShowAttachments = showAttachments;
		MarkCompleted = ReactiveCommand.CreateFromTask(execute: MarkTaskAsCompleted);
		MarkUncompleted = ReactiveCommand.CreateFromTask(execute: MarkTaskAsUncompleted);
		OnAttachedToVisualTree = ReactiveCommand.Create(execute: () => _timer.Start());
		OnDetachedFromVisualTree = ReactiveCommand.Create(execute: () => _timer.Stop());
		_isExpired = _taskToObservable.CompletionStatus == AssignedTask.TaskCompletionStatus.Expired;

		_taskToObservable.Completed += _ => RaiseCompletionStatus();
		_taskToObservable.Uncompleted += _ => RaiseCompletionStatus();
	}

	~ObservableAssignedTask() => _timer.Dispose();

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
	{
		this.RaisePropertyChanged(propertyName: nameof(ReleasedTime));
		if ((_taskToObservable.ReleasedAt - DateTime.Now).TotalSeconds <= 0)
		{
			_isExpired = true;
			RaiseCompletionStatus();
		}
	}

	public AssignedTask Observable => _taskToObservable;
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

	public bool ShowLessonName
	{
		get => _showLessonName;
		set => this.RaiseAndSetIfChanged(backingField: ref _showLessonName, newValue: value);
	}

	public bool IsExpired => _taskToObservable.CompletionStatus == AssignedTask.TaskCompletionStatus.Expired || _isExpired;
	public bool IsCompleted => _taskToObservable.CompletionStatus == AssignedTask.TaskCompletionStatus.Completed && !_isExpired;
	public bool IsUncompleted => _taskToObservable.CompletionStatus == AssignedTask.TaskCompletionStatus.Uncompleted && !_isExpired;
	public string LessonName => _taskToObservable.LessonName;
	public ReactiveCommand<Unit, Unit> ShowAttachments { get; }
	public ReactiveCommand<Unit, Unit> MarkCompleted { get; }
	public ReactiveCommand<Unit, Unit> MarkUncompleted { get; }
	public ReactiveCommand<Unit, Unit> OnAttachedToVisualTree { get; }
	public ReactiveCommand<Unit, Unit> OnDetachedFromVisualTree { get; }

	private void RaiseCompletionStatus()
	{
		this.RaisePropertyChanged(propertyName: nameof(IsExpired));
		this.RaisePropertyChanged(propertyName: nameof(IsCompleted));
		this.RaisePropertyChanged(propertyName: nameof(IsUncompleted));
	}

	private async Task MarkTaskAsUncompleted()
		=> await _taskToObservable.MarkUncompleted();

	private async Task MarkTaskAsCompleted()
		=> await _taskToObservable.MarkCompleted();
}

public static class ObservableAssignedTaskExtensions
{
	public static ObservableAssignedTask ToObservable(this AssignedTask task, bool showLessonName, ReactiveCommand<Unit, Unit> showAttachments)
	{
		ObservableAssignedTask observableTask = new ObservableAssignedTask(task: task, showLessonName: showLessonName, showAttachments: showAttachments);
		observableTask.Attachments = new List<ExtendedAttachment>(collection: task.Content.Attachments?.Select(
			selector: a => a.ToExtended()
		) ?? Enumerable.Empty<ExtendedAttachment>());
		return observableTask;
	}
}