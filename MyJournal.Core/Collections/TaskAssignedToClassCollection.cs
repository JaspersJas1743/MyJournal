using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class TaskAssignedToClassCollection :  LazyCollection<TaskAssignedToClass>
{
	#region Fields
	private readonly IFileService _fileService;
	private readonly int _subjectId;
	private readonly int _classId;
	private TaskCompletionStatus _currentStatus = TaskCompletionStatus.All;

	public static readonly TaskAssignedToClassCollection Empty = new TaskAssignedToClassCollection();
	#endregion

	#region Constructor
	private TaskAssignedToClassCollection() { }

	private TaskAssignedToClassCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<TaskAssignedToClass>> collection,
		int subjectId,
		int classId,
		int count,
		int offset
	) : base(client: client, collection: collection, count: count, offset: offset)
	{
		_fileService = fileService;
		_subjectId = subjectId;
		_classId = classId;
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
	private static async Task<IEnumerable<TaskAssignedToClass.GetCreatedTasksResponse>> LoadAll(
		ApiClient client,
		TaskCompletionStatus completionStatus,
		int classId,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<TaskAssignedToClass.GetCreatedTasksResponse>, GetAllAssignedToClassTasksRequest>(
			apiMethod: TaskControllerMethods.GetAllAssignedByClassTasks(classId: classId),
			argQuery: new GetAllAssignedToClassTasksRequest(
				CompletionStatus: completionStatus,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	private static async Task<IEnumerable<TaskAssignedToClass.GetCreatedTasksResponse>> Load(
		ApiClient client,
		TaskCompletionStatus completionStatus,
		int subjectId,
		int classId,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<TaskAssignedToClass.GetCreatedTasksResponse>, GetAssignedToClassTasksRequest>(
			apiMethod: TaskControllerMethods.GetAssignedByClassTasks(classId: classId),
			argQuery: new GetAssignedToClassTasksRequest(
				CompletionStatus: completionStatus,
				SubjectId: subjectId,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	internal static async Task<TaskAssignedToClassCollection> Create(
		ApiClient client,
		IFileService fileService,
		int subjectId,
		int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<TaskAssignedToClass.GetCreatedTasksResponse> tasks = subjectId == 0 ?
			await LoadAll(
				client: client,
				completionStatus: TaskCompletionStatus.All,
				classId: classId,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			) : await Load(
				client: client,
				completionStatus: TaskCompletionStatus.All,
				subjectId: subjectId,
				classId: classId,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			);
		return new TaskAssignedToClassCollection(
			client: client,
			fileService: fileService,
			collection: new AsyncLazy<List<TaskAssignedToClass>>(valueFactory: async () => new List<TaskAssignedToClass>(collection: await Task.WhenAll(
				tasks: tasks.Select(selector: async t => await TaskAssignedToClass.Create(
					client: client,
					fileService: fileService,
					response: t
				))
			))),
			classId: classId,
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
		await base.Append(instance: await TaskAssignedToClass.Create(
			client: Client,
			fileService: _fileService,
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
		await base.Insert(index: index, instance: await TaskAssignedToClass.Create(
			client: Client,
			fileService: _fileService,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<TaskAssignedToClass.GetCreatedTasksResponse> tasks = _subjectId == 0 ?
			await LoadAll(
				client: Client,
				completionStatus: _currentStatus,
				classId: _classId,
				offset: Offset,
				count: Count,
				cancellationToken: cancellationToken
			) : await Load(
				client: Client,
				completionStatus: _currentStatus,
				classId: _classId,
				subjectId: _subjectId,
				offset: Offset,
				count: Count,
				cancellationToken: cancellationToken
			);
		List<TaskAssignedToClass> collection = await Collection;
		collection.AddRange(collection: await Task.WhenAll(tasks: tasks.Select(
			selector: async t => await TaskAssignedToClass.Create(
				client: Client,
				fileService: _fileService,
				response: t
			)
		)));
		Offset = collection.Count;
	}
	#endregion
	#endregion
}