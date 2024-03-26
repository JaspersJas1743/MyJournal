using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class CreatedTaskCollection : LazyCollection<CreatedTask>
{
	private TaskCompletionStatus _currentStatus = TaskCompletionStatus.All;
	private readonly int _subjectId;

	#region Constructor
	private CreatedTaskCollection(
		ApiClient client,
		IEnumerable<CreatedTask> collection,
		int subjectId,
		int count
	) : base(
		client: client,
		collection: collection,
		count: count
	)
	{
		_subjectId = subjectId;
	}
	#endregion

	#region Enum

	public enum TaskCompletionStatus
	{
		All,
		Expired,
		NotExpired
	}
	#endregion

	#region Records
	public sealed record GetAssignedToClassTasksRequest(TaskCompletionStatus CompletionStatus, int SubjectId, int Offset, int Count);
	public sealed record GetAllAssignedToClassTasksRequest(TaskCompletionStatus CompletionStatus, int Offset, int Count);
	#endregion

	#region Methods
	#region Static
	private static async Task<IEnumerable<CreatedTask.GetCreatedTasksResponse>> LoadAll(
		ApiClient client,
		TaskCompletionStatus completionStatus,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<CreatedTask.GetCreatedTasksResponse>, GetAllAssignedToClassTasksRequest>(
			apiMethod: TaskControllerMethods.GetAllCreatedTasks,
			argQuery: new GetAllAssignedToClassTasksRequest(
				CompletionStatus: completionStatus,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	private static async Task<IEnumerable<CreatedTask.GetCreatedTasksResponse>> Load(
		ApiClient client,
		TaskCompletionStatus completionStatus,
		int subjectId,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<CreatedTask.GetCreatedTasksResponse>, GetAssignedToClassTasksRequest>(
			apiMethod: TaskControllerMethods.GetCreatedTasks,
			argQuery: new GetAssignedToClassTasksRequest(
				CompletionStatus: completionStatus,
				SubjectId: subjectId,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	internal static async Task<CreatedTaskCollection> Create(
		ApiClient client,
		int subjectId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<CreatedTask.GetCreatedTasksResponse> tasks = subjectId == 0 ?
			await LoadAll(
				client: client,
				completionStatus: TaskCompletionStatus.All,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			) : await Load(
				client: client,
				completionStatus: TaskCompletionStatus.All,
				subjectId: subjectId,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			);
		return new CreatedTaskCollection(
			client: client,
			collection: tasks.Select(selector: t => CreatedTask.Create(client: client, response: t)),
			subjectId: subjectId,
			count: basedCount
		);
	}
	#endregion

	#region Instance
	public async Task SetCompletionStatus(
		TaskCompletionStatus status,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		_currentStatus = status;
		await Load(cancellationToken: cancellationToken);
	}
	#endregion

	#region LazyCollection<TaskAssignedToClass>
	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await CreatedTask.Create(
			client: _client,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	internal override async Task Insert(
		int index,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Insert(index: index, instance: await CreatedTask.Create(
			client: _client,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<CreatedTask.GetCreatedTasksResponse> tasks = _subjectId == 0 ?
			await LoadAll(
				client: _client,
				completionStatus: _currentStatus,
				offset: _offset,
				count: _count,
				cancellationToken: cancellationToken
			) : await Load(
				client: _client,
				completionStatus: _currentStatus,
				subjectId: _subjectId,
				offset: _offset,
				count: _count,
				cancellationToken: cancellationToken
			);
		_collection.Value.AddRange(collection: tasks.Select(selector: t => CreatedTask.Create(
			client: _client,
			response: t
		)));
		_offset = _collection.Value.Count;
	}
	#endregion
	#endregion
}