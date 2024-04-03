using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class CreatedTask : BaseTask
{
	#region Fields
	private readonly ApiClient _client;
	#endregion

	#region Constructors
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

	#region Events
	public event CompletedTaskHandler Completed;
	public event UncompletedTaskHandler Uncompleted;
	public event CreatedTaskHandler Created;
	#endregion

	#region Methods
	#region Static
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

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await ChangeCompletionData();
		Completed?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await ChangeCompletionData();
		Uncompleted?.Invoke(e: e);
	}

	internal void OnCreatedTask(CreatedTaskEventArgs e)
		=> Created?.Invoke(e: e);
	#endregion
	#endregion
}