using System.ComponentModel;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class CreatedTaskCollection : LazyCollection<CreatedTask>
{
	#region Fields
	private readonly IFileService _fileService;
	private readonly int _subjectId;
	private readonly int _classId;
	private TaskCompletionStatus _currentStatus = TaskCompletionStatus.All;

	public static readonly CreatedTaskCollection Empty = new CreatedTaskCollection();
	#endregion

	#region Constructor
	private CreatedTaskCollection() { }

	private CreatedTaskCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<CreatedTask>> collection,
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
		[Description(description: "Все задачи")]
		All,
		[Description(description: "Завершенные")]
		Expired,
		[Description(description: "Открытые")]
		NotExpired
	}
	#endregion

	#region Records
	public sealed record GetCreatedTasksRequest(TaskCompletionStatus CompletionStatus, int SubjectId, int ClassId, int Offset, int Count);
	public sealed record GetAllCreatedTasksRequest(TaskCompletionStatus CompletionStatus, int Offset, int Count);
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
		return await client.GetAsync<IEnumerable<CreatedTask.GetCreatedTasksResponse>, GetAllCreatedTasksRequest>(
			apiMethod: TaskControllerMethods.GetAllCreatedTasks,
			argQuery: new GetAllCreatedTasksRequest(
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
		int classId,
		int offset,
		int count,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<CreatedTask.GetCreatedTasksResponse>, GetCreatedTasksRequest>(
			apiMethod: TaskControllerMethods.GetCreatedTasks,
			argQuery: new GetCreatedTasksRequest(
				CompletionStatus: completionStatus,
				SubjectId: subjectId,
				ClassId: classId,
				Offset: offset,
				Count: count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	internal static async Task<CreatedTaskCollection> Create(
		ApiClient client,
		IFileService fileService,
		int subjectId,
		int classId = 0,
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
				classId: classId,
				offset: basedOffset,
				count: basedCount,
				cancellationToken: cancellationToken
			);
		return new CreatedTaskCollection(
			client: client,
			fileService: fileService,
			collection: new AsyncLazy<List<CreatedTask>>(valueFactory: async () => new List<CreatedTask>(collection: await Task.WhenAll(
				tasks: tasks.Select(selector: async t => await CreatedTask.Create(
					client: client,
					fileService: fileService,
					response: t
				))
			))),
			subjectId: subjectId,
			classId: classId,
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
		await base.Insert(index: index, instance: await CreatedTask.Create(
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
				classId: _classId,
				offset: Offset,
				count: Count,
				cancellationToken: cancellationToken
			);
		List<CreatedTask> collection = await Collection;
		collection.AddRange(collection: await Task.WhenAll(tasks: tasks.Select(
			selector: t => CreatedTask.Create(client: Client, fileService: _fileService, response: t)
		)));
		Offset = collection.Count;
	}
	#endregion
	#endregion
}