using System;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class CreatedTask : ReactiveObject
{
	private readonly MyJournal.Core.SubEntities.CreatedTask? _createdTask;
	private readonly TaskAssignedToClass? _taskAssignedToClass;

	public CreatedTask(MyJournal.Core.SubEntities.CreatedTask? createdTask)
	{
		if (createdTask is null)
			return;

		_createdTask = createdTask;

		createdTask.Completed += e => Completed?.Invoke(e: e);
		createdTask.Uncompleted += e => Uncompleted?.Invoke(e: e);
	}

	public CreatedTask(TaskAssignedToClass? taskAssignedToClass)
	{
		if (taskAssignedToClass is null)
			return;

		_taskAssignedToClass = taskAssignedToClass;

		taskAssignedToClass.Completed += e => Completed?.Invoke(e: e);
		taskAssignedToClass.Uncompleted += e => Uncompleted?.Invoke(e: e);
	}

	public int Id => _createdTask?.Id ?? _taskAssignedToClass!.Id;
	public DateTime ReleasedAt => _createdTask?.ReleasedAt ?? _taskAssignedToClass!.ReleasedAt;
	public TaskContent Content => _createdTask?.Content ?? _taskAssignedToClass!.Content;
	public string ClassName => _createdTask?.ClassName ?? _taskAssignedToClass!.ClassName;
	public int CountOfCompletedTask => _createdTask?.CountOfCompletedTask ?? _taskAssignedToClass!.CountOfCompletedTask;
	public int CountOfUncompletedTask => _createdTask?.CountOfUncompletedTask ?? _taskAssignedToClass!.CountOfUncompletedTask;
	public string LessonName => _createdTask?.LessonName ?? _taskAssignedToClass!.LessonName;

	public event CompletedTaskHandler Completed;
	public event UncompletedTaskHandler Uncompleted;
}