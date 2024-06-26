using System.ComponentModel;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class AssignedTaskCollection : LazyCollection<AssignedTask>
{
	private readonly IFileService _fileService;
	private readonly int _subjectId;

	private AssignedTaskCompletionStatus _currentStatus = AssignedTaskCompletionStatus.All;

	public static readonly AssignedTaskCollection Empty = new AssignedTaskCollection();

	#region Constructor
	private AssignedTaskCollection() { }

	private AssignedTaskCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<AssignedTask>> collection,
		int subjectId,
		int count,
		int offset
	) : base(client: client, collection: collection, count: count, offset: offset)
	{
		_fileService = fileService;
		_subjectId = subjectId;
	}
	#endregion

	#region Enum
	public enum AssignedTaskCompletionStatus
	{
		[Description(description: "Все задачи")]
		All,
		[Description(description: "Открытые")]
		Uncompleted,
		[Description(description: "Выполненные")]
		Completed,
		[Description(description: "Завершенные")]
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
		IFileService fileService,
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
			fileService: fileService,
			collection: new AsyncLazy<List<AssignedTask>>(valueFactory: async () => new List<AssignedTask>(collection: await Task.WhenAll(
				tasks: tasks.Select(selector: async t => await AssignedTask.Create(
					client: client,
					fileService: fileService,
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
		AssignedTaskCompletionStatus status,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (_currentStatus == status)
			return;

		await Clear(cancellationToken: cancellationToken);
		_currentStatus = status;
		await Load(cancellationToken: cancellationToken);
	}
	#endregion

	#region LazyCollection<AssignedTask>
	internal override async Task Add(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<AssignedTask> collection = await Collection;
		await Append(id: id, cancellationToken: cancellationToken);
		collection.Sort(comparison: (first, second) => first.ReleasedAt.CompareTo(value: second.ReleasedAt));
	}

	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await AssignedTask.Create(
			client: Client,
			id: id,
			fileService: _fileService,
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
			fileService: _fileService,
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
			selector: async t => await AssignedTask.Create(client: Client, fileService: _fileService, response: t)
		)));
		Offset = collection.Count;
	}
	#endregion
	#endregion
}