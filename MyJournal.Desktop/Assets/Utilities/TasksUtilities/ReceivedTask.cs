using System;
using System.Threading.Tasks;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ReceivedTask : ReactiveObject
{
	private readonly AssignedTask? _assignedTask;
	private readonly TaskAssignedToWard? _taskAssignedToWard;

	public ReceivedTask(AssignedTask? assignedTask)
	{
		if (assignedTask is null)
			return;

		_assignedTask = assignedTask;
		IsReceivedToWard = false;

		assignedTask.Completed += e => Completed?.Invoke(e: e);
		assignedTask.Uncompleted += e => Uncompleted?.Invoke(e: e);
	}

	public ReceivedTask(TaskAssignedToWard? taskAssignedToWard)
	{
		if (taskAssignedToWard is null)
			return;

		_taskAssignedToWard = taskAssignedToWard;
		IsReceivedToWard = true;

		taskAssignedToWard.Completed += e => Completed?.Invoke(e: e);
		taskAssignedToWard.Uncompleted += e => Uncompleted?.Invoke(e: e);
	}

	public int Id => _assignedTask?.Id ?? _taskAssignedToWard!.Id;
	public DateTime ReleasedAt => _assignedTask?.ReleasedAt ?? _taskAssignedToWard!.ReleasedAt;
	public TaskContent Content => _assignedTask?.Content ?? _taskAssignedToWard!.Content;
	public TaskCompletionStatus CompletionStatus => Enum.Parse<TaskCompletionStatus>(
		value: _assignedTask?.CompletionStatus.ToString() ?? _taskAssignedToWard!.CompletionStatus.ToString()
	);
	public string LessonName => _assignedTask?.LessonName ?? _taskAssignedToWard!.LessonName;
	public bool IsReceivedToWard { get; init; }

	public event CompletedTaskHandler Completed;
	public event UncompletedTaskHandler Uncompleted;

	public enum TaskCompletionStatus
	{
		Uncompleted,
		Completed,
		Expired
	}

	public async Task MarkCompleted()
	{
		if (_assignedTask is null)
			return;

		await _assignedTask.MarkCompleted();
	}

	public async Task MarkUncompleted()
	{
		if (_assignedTask is null)
			return;

		await _assignedTask.MarkUncompleted();
	}
}