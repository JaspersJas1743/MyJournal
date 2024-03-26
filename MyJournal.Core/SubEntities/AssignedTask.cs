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

	public TaskCompletionStatus CompletionStatus { get; init; }
	public string LessonName { get; init; }

	public enum TaskCompletionStatus { Uncompleted, Completed, Expired }

	internal sealed record GetAssignedTaskResponse(int TaskId, string LessonName, DateTime ReleasedAt, TaskContent Content, TaskCompletionStatus CompletionStatus);

	internal static AssignedTask Create(
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
}