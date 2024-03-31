using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class TaskAssignedToClass : BaseTask
{
	#region Fields
	private readonly ApiClient _client;
	#endregion

	#region Constructors
	private TaskAssignedToClass(
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
	#endregion

	#region Properties
	public string LessonName { get; init; }
	public string ClassName { get; init; }
	public int CountOfCompletedTask { get; private set; }
	public int CountOfUncompletedTask { get; private set; }
	#endregion

	#region Records
	internal sealed record GetCreatedTasksResponse(int TaskId, string ClassName, string LessonName, DateTime ReleasedAt, TaskContent Content, int CountOfCompletedTask, int CountOfUncompletedTask);
	#endregion

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

	#region Methods
	#region Static
	internal static async Task<TaskAssignedToClass> Create(
		ApiClient client,
		GetCreatedTasksResponse response
	) => new TaskAssignedToClass(client: client, response: response);

	internal static async Task<TaskAssignedToClass> Create(
		ApiClient client,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetCreatedTasksResponse response = await client.GetAsync<GetCreatedTasksResponse>(
			apiMethod: TaskControllerMethods.GetCreatedTaskById(taskId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaskAssignedToClass(client: client, response: response);
	}
	#endregion

	#region Instance
	private async Task ChangeCompletionData()
	{
		GetCreatedTasksResponse response = await _client.GetAsync<GetCreatedTasksResponse>(
			apiMethod: TaskControllerMethods.GetCreatedTaskById(taskId: Id)
		) ?? throw new InvalidOperationException();
		CountOfCompletedTask = response.CountOfCompletedTask;
		CountOfUncompletedTask = response.CountOfUncompletedTask;
	}

	internal async Task OnCompletedTask(CompletedEventArgs e)
	{
		await ChangeCompletionData();
		Completed?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedEventArgs e)
	{
		await ChangeCompletionData();
		Uncompleted?.Invoke(e: e);
	}

	internal void OnCreatedTask(CreatedEventArgs e)
		=> Created?.Invoke(e: e);
	#endregion
	#endregion
}