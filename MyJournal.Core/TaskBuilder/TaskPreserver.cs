using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.TaskBuilder;

public sealed class TaskPreserver : ITaskPreserver
{
	private readonly ApiClient _client;
	private readonly TaskContent _content;
	private readonly int _subjectId;
	private readonly int _classId;
	private readonly DateTime _releasedAt;

	private TaskPreserver(
		ApiClient client,
		TaskContent content,
		int subjectId,
		int classId,
		DateTime releasedAt
	)
	{
		_client = client;
		_content = content;
		_subjectId = subjectId;
		_classId = classId;
		_releasedAt = releasedAt;
	}

	private sealed record CreateTasksRequest(int SubjectId, int ClassId, TaskContent Content, DateTime ReleasedAt);
	private sealed record CreateTasksResponse(string Message);

	public async Task<string> Save(CancellationToken cancellationToken = default(CancellationToken))
	{
		CreateTasksResponse response = await _client.PostAsync<CreateTasksResponse, CreateTasksRequest>(
			apiMethod: TaskControllerMethods.CreateTask,
			arg: new CreateTasksRequest(
				SubjectId: _subjectId,
				ClassId: _classId,
				Content: _content,
				ReleasedAt: _releasedAt
			),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}

	internal static TaskPreserver Create(
		ApiClient client,
		TaskContent content,
		int subjectId,
		int classId,
		DateTime releasedAt,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaskPreserver(
			client: client,
			content: content,
			subjectId: subjectId,
			classId: classId,
			releasedAt: releasedAt
		);
	}
}