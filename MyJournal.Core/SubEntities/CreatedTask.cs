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
	public int CountOfCompletedTask { get; init; }
	public int CountOfUncompletedTask { get; init; }

	internal sealed record GetCreatedTasksResponse(int TaskId, string ClassName, string LessonName, DateTime ReleasedAt, TaskContent Content, int CountOfCompletedTask, int CountOfUncompletedTask);

	internal static CreatedTask Create(
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
}