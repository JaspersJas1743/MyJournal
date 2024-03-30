using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class AssignedTask : BaseTask
{
	private readonly ApiClient _client;

	private AssignedTask(
		ApiClient client,
		GetAssignedTaskResponse response
	)
	{
		_client = client;
		Id = response.TaskId;
		ReleasedAt = response.ReleasedAt;
		Content = response.Content;
		CompletionStatus = response.CompletionStatus;
		LessonName = response.LessonName;
	}

	public TaskCompletionStatus CompletionStatus { get; private set; }
	public string LessonName { get; init; }

	public enum TaskCompletionStatus { Uncompleted, Completed, Expired }

	internal sealed record GetAssignedTaskResponse(int TaskId, string LessonName, DateTime ReleasedAt, TaskContent Content, TaskCompletionStatus CompletionStatus);

	#region Classes
	public sealed class CompletedEventArgs : EventArgs;
	public sealed class UncompletedEventArgs : EventArgs;
	public sealed class CreatedEventArgs : EventArgs;
	#endregion

	#region Delegates
	public delegate void CompletedHandler(CompletedEventArgs e);
	public delegate void UncompletedHandler(UncompletedEventArgs e);
	public delegate void CreatedHandler(CreatedEventArgs e);
	#endregion

	#region Events
	public event CompletedHandler Completed;
	public event UncompletedHandler Uncompleted;
	public event CreatedHandler Created;
	#endregion

	internal static async Task<AssignedTask> Create(
		ApiClient client,
		GetAssignedTaskResponse response
	) => new AssignedTask(client: client, response: response);

	internal static async Task<AssignedTask> Create(
		ApiClient client,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetAssignedTaskResponse response = await client.GetAsync<GetAssignedTaskResponse>(
			apiMethod: TaskControllerMethods.GetAssignedTaskById(taskId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new AssignedTask(client: client, response: response);
	}

	public async Task MarkCompleted(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PutAsync(
			apiMethod: TaskControllerMethods.ChangeCompletionStatusForTask(
				taskId: Id,
				completionStatus: TaskControllerMethods.CompletionStatus.Completed
			),
			cancellationToken: cancellationToken
		);
	}

	public async Task MarkUncompleted(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PutAsync(
			apiMethod: TaskControllerMethods.ChangeCompletionStatusForTask(
				taskId: Id,
				completionStatus: TaskControllerMethods.CompletionStatus.Uncompleted
			),
			cancellationToken: cancellationToken
		);
	}

	internal async Task OnCompletedTask(CompletedEventArgs e)
	{
		CompletionStatus = TaskCompletionStatus.Completed;
		Completed?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedEventArgs e)
	{
		CompletionStatus = TaskCompletionStatus.Uncompleted;
		Uncompleted?.Invoke(e: e);
	}

	internal void OnCreatedTask(CreatedEventArgs e)
		=> Created?.Invoke(e: e);
}