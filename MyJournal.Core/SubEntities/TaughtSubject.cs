using MyJournal.Core.Collections;
using MyJournal.Core.TaskBuilder;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
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
	private readonly IFileService _fileService;
	private readonly AsyncLazy<CreatedTaskCollection> _tasks;
	#endregion

	#region Constructors
	private TaughtSubject(
		IFileService fileService,
		AsyncLazy<CreatedTaskCollection> tasks
	)
	{
		_fileService = fileService;
		_tasks = tasks;
	}

	private TaughtSubject(
		IFileService fileService,
		string name,
		AsyncLazy<CreatedTaskCollection> tasks
	) : this(tasks: tasks, fileService: fileService)
	{
		Name = name;
	}

	private TaughtSubject(
		IFileService fileService,
		TaughtSubjectResponse response,
		AsyncLazy<CreatedTaskCollection> tasks
	) : this(tasks: tasks, fileService: fileService)
	{
		Id = response.Id;
		Name = response.Name;
		Class = response.Class;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	public TaughtClass Class { get; init; }
	public bool TasksAreCreated => _tasks.IsValueCreated;
	#endregion

	#region Records
	internal sealed record TaughtSubjectResponse(int Id, string Name, TaughtClass Class);
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
		return new TaughtSubject(fileService: fileService, response: response, tasks: new AsyncLazy<CreatedTaskCollection>(
			valueFactory: async () => await CreatedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static async Task<TaughtSubject> Create(
		ApiClient client,
		IFileService fileService,
		string name,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(fileService: fileService, name: name, tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory:
			async () => await CreatedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static TaughtSubject CreateWithoutTasks(
		IFileService fileService,
		string name
	) => new TaughtSubject(fileService: fileService, name: name, tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory: async () => null));

	internal static TaughtSubject CreateWithoutTasks(
		IFileService fileService,
		TaughtSubjectResponse response
	) => new TaughtSubject(fileService: fileService, response: response, tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory: async () => null));
	#endregion

	#region Instance
	public async Task<CreatedTaskCollection> GetTasks()
		=> await _tasks;

	public IInitTaskBuilder CreateTask()
		=> InitTaskBuilder.Create(fileService: _fileService);

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (CreatedTask createdTask in collection.Where(predicate: task => task.Id == e.TaskId))
				await createdTask.OnCompletedTask(e: new CreatedTask.CompletedEventArgs());
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (CreatedTask createdTask in collection.Where(predicate: task => task.Id == e.TaskId))
				await createdTask.OnUncompletedTask(e: new CreatedTask.UncompletedEventArgs());
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			await foreach (CreatedTask createdTask in collection.Where(predicate: task => task.Id == e.TaskId))
				createdTask.OnCreatedTask(e: new CreatedTask.CreatedEventArgs());
		});

		CreatedTask?.Invoke(e: e);
	}

	private async Task InvokeIfTasksAreCreated(
		Func<CreatedTaskCollection, Task> invocation
	)
	{
		if (!_tasks.IsValueCreated)
			return;

		CreatedTaskCollection collection = await _tasks;
		await invocation(arg: collection);
	}
	#endregion
	#endregion
}