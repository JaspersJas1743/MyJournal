using MyJournal.Core.Collections;
using MyJournal.Core.TaskBuilder;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
}

public sealed class TaughtSubject : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly Lazy<CreatedTaskCollection> _tasks;
	#endregion

	#region Constructors
	private TaughtSubject(
		ApiClient client,
		IFileService fileService,
		Lazy<CreatedTaskCollection> tasks
	)
	{
		_client = client;
		_fileService = fileService;
		_tasks = tasks;
	}

	private TaughtSubject(
		ApiClient client,
		IFileService fileService,
		string name,
		Lazy<CreatedTaskCollection> tasks
	) : this(client: client, tasks: tasks, fileService: fileService)
	{
		Name = name;
		IsFirst = true;
	}

	private TaughtSubject(
		ApiClient client,
		IFileService fileService,
		TaughtSubjectResponse response,
		Lazy<CreatedTaskCollection> tasks
	) : this(client: client, tasks: tasks, fileService: fileService)
	{
		Id = response.Id;
		Name = response.Name;
		Class = response.Class;
		IsFirst = false;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	public TaughtClass Class { get; init; }
	internal bool IsFirst { get; init; }
	#endregion

	#region Records
	internal sealed record TaughtSubjectResponse(int Id, string Name, TaughtClass Class);
	#endregion

	#region Properties
	public CreatedTaskCollection Tasks => _tasks.Value;
	#endregion

	#region Classes
	public sealed class CompletedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	public sealed class UncompletedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	public sealed class CreatedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	#endregion

	#region Delegates
	public delegate void CompletedTaskHandler(CompletedTaskEventArgs e);
	public delegate void UncompletedTaskHandler(UncompletedTaskEventArgs e);
	public delegate void CreatedTaskHandler(CreatedTaskEventArgs e);
	#endregion

	#region Events
	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;
	public event CreatedTaskHandler CreatedTask;
	#endregion

	#region Methods
	#region Static
	internal static async Task<TaughtSubject> Create(
		ApiClient client,
		IFileService fileService,
		TaughtSubjectResponse response,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(client: client, fileService: fileService, response: response, tasks: new Lazy<CreatedTaskCollection>(value:
			CreatedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static async Task<TaughtSubject> Create(
		ApiClient client,
		IFileService fileService,
		string name,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(client: client, fileService: fileService, name: name, tasks: new Lazy<CreatedTaskCollection>(value:
			CreatedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}
	#endregion

	#region Instance
	public IInitTaskBuilder CreateTask()
		=> InitTaskBuilder.Create(fileService: _fileService);

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		foreach (CreatedTask task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			await task.OnCompletedTask(e: new CreatedTask.CompletedEventArgs());

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		foreach (CreatedTask task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			await task.OnUncompletedTask(e: new CreatedTask.UncompletedEventArgs());

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await Tasks.Append(id: e.TaskId);
		foreach (CreatedTask task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			task.OnCreatedTask(e: new CreatedTask.CreatedEventArgs());

		CreatedTask?.Invoke(e: e);
	}
	#endregion
	#endregion
}