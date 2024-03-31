using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class AssignedTaskCollection : LazyCollection<AssignedTask>
{
	private AssignedTaskCompletionStatus _currentStatus = AssignedTaskCompletionStatus.All;
	private readonly int _subjectId;

	#region Constructor
	private AssignedTaskCollection(
		ApiClient client,
		AsyncLazy<List<AssignedTask>> collection,
		int subjectId,
		int count,
		int offset
	) : base(client: client, collection: collection, count: count, offset: offset)
	{
		_subjectId = subjectId;
	}
	#endregion

	#region Enum
	public enum AssignedTaskCompletionStatus
	{
		All,
		Uncompleted,
		Completed,
		Expired
	}
	#endregion

	#region Records
	public sealed record GetAllAssignedTasksRequest(AssignedTaskCompletionStatus CompletionStatus, int Offset, int Count);
	public sealed record GetAssignedTasksRequest(AssignedTaskCompletionStatus CompletionStatus, int SubjectId, int Offset, int Count);
	#endregion

	#region Methods
	#region Static
	private static async Task<IEnumerable<AssignedTask.GetAssignedTaskResponse>> LoadAll(
		ApiClient client,
		AssignedTaskCompletionStatus completionStatus,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<AssignedTask.GetAssignedTaskResponse>, GetAllAssignedTasksRequest>(
			apiMethod: TaskControllerMethods.GetAllAssignedTasks,
			argQuery: new GetAllAssignedTasksRequest(
				CompletionStatus: completionStatus,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	private static async Task<IEnumerable<AssignedTask.GetAssignedTaskResponse>> Load(
		ApiClient client,
		AssignedTaskCompletionStatus completionStatus,
		int subjectId,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<AssignedTask.GetAssignedTaskResponse>, GetAssignedTasksRequest>(
			apiMethod: TaskControllerMethods.GetAssignedTasks,
			argQuery: new GetAssignedTasksRequest(
				CompletionStatus: completionStatus,
				SubjectId: subjectId,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	internal static async Task<AssignedTaskCollection> Create(
		ApiClient client,
		int subjectId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<AssignedTask.GetAssignedTaskResponse> tasks = subjectId == 0 ?
			await LoadAll(
				client: client,
				completionStatus: AssignedTaskCompletionStatus.All,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			) : await Load(
				client: client,
				completionStatus: AssignedTaskCompletionStatus.All,
				subjectId: subjectId,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			);
		return new AssignedTaskCollection(
			client: client,
			collection: new AsyncLazy<List<AssignedTask>>(valueFactory: async () => new List<AssignedTask>(collection: await Task.WhenAll(
				tasks: tasks.Select(selector: async t => await AssignedTask.Create(client: client, response: t))
			))),
			subjectId: subjectId,
			count: basedCount,
			offset: tasks.Count()
		);
	}
	#endregion

	#region Instance
	public async Task SetCompletionStatus(
		AssignedTaskCompletionStatus status,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		_currentStatus = status;
		await Load(cancellationToken: cancellationToken);
	}
	#endregion

	#region LazyCollection<AssignedTask>
	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await AssignedTask.Create(
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
		await base.Insert(index: index, instance: await AssignedTask.Create(
			client: Client,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<AssignedTask.GetAssignedTaskResponse> tasks = _subjectId == 0 ?
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
		List<AssignedTask> collection = await Collection;
		collection.AddRange(collection: await Task.WhenAll(tasks: tasks.Select(
			selector: async t => await AssignedTask.Create(client: Client, response: t)
		)));
		Offset = collection.Count;
	}
	#endregion
	#endregion
}