using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class CreatedTask : BaseTask
{
	private readonly ApiClient _client;

	private CreatedTask(
		ApiClient client,
		GetCreatedTasksResponse response
	)
	{
		_client = client;
		Id = response.TaskId;
		ReleasedAt = response.ReleasedAt;
		Content = response.Content;
		ClassName = response.ClassName;
		LessonName = response.LessonName;
		CountOfCompletedTask = response.CountOfCompletedTask;
		CountOfUncompletedTask = response.CountOfUncompletedTask;
	}

	public string LessonName { get; init; }
	public string ClassName { get; init; }
	public int CountOfCompletedTask { get; private set; }
	public int CountOfUncompletedTask { get; private set; }

	internal sealed record GetCreatedTasksResponse(int TaskId, string ClassName, string LessonName, DateTime ReleasedAt, TaskContent Content, int CountOfCompletedTask, int CountOfUncompletedTask);

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

	internal static async Task<CreatedTask> Create(
		ApiClient client,
		GetCreatedTasksResponse response
	) => new CreatedTask(client: client, response: response);

	internal static async Task<CreatedTask> Create(
		ApiClient client,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetCreatedTasksResponse response = await client.GetAsync<GetCreatedTasksResponse>(
			apiMethod: TaskControllerMethods.GetCreatedTaskById(taskId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new CreatedTask(client: client, response: response);
	}

	internal async Task OnCompletedTask(CompletedEventArgs e)
	{
		CountOfCompletedTask += 1;
		CountOfUncompletedTask -= 1;
		Completed?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedEventArgs e)
	{
		CountOfCompletedTask -= 1;
		CountOfUncompletedTask += 1;
		Uncompleted?.Invoke(e: e);
	}

	internal void OnCreatedTask(CreatedEventArgs e)
		=> Created?.Invoke(e: e);
}