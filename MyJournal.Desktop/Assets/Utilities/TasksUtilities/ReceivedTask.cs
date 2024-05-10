using System;
using System.Threading.Tasks;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ReceivedTask : ReactiveObject
{
	private readonly AssignedTask? _assignedTask;

	public ReceivedTask(AssignedTask? assignedTask)
	{
		if (assignedTask is null)
			return;

		_assignedTask = assignedTask;
		Id = assignedTask.Id;
		ReleasedAt = assignedTask.ReleasedAt;
		Content = assignedTask.Content;
		CompletionStatus = Enum.Parse<TaskCompletionStatus>(value: assignedTask.CompletionStatus.ToString());
		LessonName = assignedTask.LessonName;
		IsReceivedToWard = false;

		assignedTask.Completed += e =>
		{
			CompletionStatus = TaskCompletionStatus.Completed;
			Completed?.Invoke(e: e);
		};
		assignedTask.Uncompleted += e =>
		{
			CompletionStatus = TaskCompletionStatus.Uncompleted;
			Uncompleted?.Invoke(e: e);
		};
	}

	public ReceivedTask(TaskAssignedToWard? taskAssignedToWard)
	{
		if (taskAssignedToWard is null)
			return;

		Id = taskAssignedToWard.Id;
		ReleasedAt = taskAssignedToWard.ReleasedAt;
		Content = taskAssignedToWard.Content;
		CompletionStatus = Enum.Parse<TaskCompletionStatus>(value: taskAssignedToWard.CompletionStatus.ToString());
		LessonName = taskAssignedToWard.LessonName;
		IsReceivedToWard = true;

		taskAssignedToWard.Completed += e => Completed?.Invoke(e: e);
		taskAssignedToWard.Uncompleted += e => Uncompleted?.Invoke(e: e);
	}

	public int Id { get; init; }
	public DateTime ReleasedAt { get; init; }
	public TaskContent Content { get; init; }
	public TaskCompletionStatus CompletionStatus { get; private set; }
	public string LessonName { get; init; }
	public bool IsReceivedToWard { get; }

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