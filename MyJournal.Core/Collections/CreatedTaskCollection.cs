using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class CreatedTaskCollection : LazyCollection<CreatedTask>
{
	#region Fields
	private TaskCompletionStatus _currentStatus = TaskCompletionStatus.All;
	private readonly int _subjectId;

	public static readonly CreatedTaskCollection Empty = new CreatedTaskCollection(
		client: ApiClient.Empty,
		subjectId: -1,
		count: -1,
		offset: -1,
		collection: new AsyncLazy<List<CreatedTask>>(valueFactory: () => new List<CreatedTask>())
	);
	#endregion

	#region Constructor
	private CreatedTaskCollection(
		ApiClient client,
		AsyncLazy<List<CreatedTask>> collection,
		int subjectId,
		int count,
		int offset
	) : base(client: client, collection: collection, count: count, offset: offset)
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
			collection: new AsyncLazy<List<CreatedTask>>(valueFactory: async () => new List<CreatedTask>(collection: await Task.WhenAll(
				tasks: tasks.Select(selector: async t => await CreatedTask.Create(
					client: client,
					response: t
				))
			))),
			subjectId: subjectId,
			count: basedCount,
			offset: tasks.Count()
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
			client: Client,
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
			client: Client,
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
				client: Client,
				completionStatus: _currentStatus,
				offset: Offset,
				count: Count,
				cancellationToken: cancellationToken
			) : await Load(
				client: Client,
				completionStatus: _currentStatus,
				subjectId: _subjectId,
				offset: Offset,
				count: Count,
				cancellationToken: cancellationToken
			);
		List<CreatedTask> collection = await Collection;
		collection.AddRange(collection: await Task.WhenAll(tasks: tasks.Select(
			selector: t => CreatedTask.Create(client: Client, response: t)
		)));
		Offset = collection.Count;
	}
	#endregion
	#endregion
}