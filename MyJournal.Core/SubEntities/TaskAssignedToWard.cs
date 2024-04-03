using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class TaskAssignedToWard : BaseTask
{
	#region Constructors
	private TaskAssignedToWard(
		ApiClient client,
		GetAssignedTaskResponse response
	)
	{
		Id = response.TaskId;
		ReleasedAt = response.ReleasedAt;
		Content = response.Content;
		CompletionStatus = response.CompletionStatus;
		LessonName = response.LessonName;
	}
	#endregion

	#region Properties
	public TaskCompletionStatus CompletionStatus { get; private set; }
	public string LessonName { get; init; }
	#endregion

	#region Enum
	public enum TaskCompletionStatus
	{
		Uncompleted,
		Completed,
		Expired
	}
	#endregion

	#region Records
	internal sealed record GetAssignedTaskResponse(int TaskId, string LessonName, DateTime ReleasedAt, TaskContent Content, TaskCompletionStatus CompletionStatus);
	#endregion

	#region Events
	public event CompletedTaskHandler Completed;
	public event UncompletedTaskHandler Uncompleted;
	public event CreatedTaskHandler Created;
	#endregion

	#region Methods
	#region Static
	internal static async Task<TaskAssignedToWard> Create(
		ApiClient client,
		GetAssignedTaskResponse response
	) => new TaskAssignedToWard(client: client, response: response);

	internal static async Task<TaskAssignedToWard> Create(
		ApiClient client,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetAssignedTaskResponse response = await client.GetAsync<GetAssignedTaskResponse>(
			apiMethod: TaskControllerMethods.GetAssignedTaskById(taskId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaskAssignedToWard(client: client, response: response);
	}
	#endregion

	#region Instance
	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		CompletionStatus = TaskCompletionStatus.Completed;
		Completed?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		CompletionStatus = TaskCompletionStatus.Uncompleted;
		Uncompleted?.Invoke(e: e);
	}

	internal void OnCreatedTask(CreatedTaskEventArgs e)
		=> Created?.Invoke(e: e);
	#endregion
	#endregion
}