using MyJournal.Core.Collections;
using MyJournal.Core.TaskBuilder;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtSubject : ISubEntity
{
	#region Fields
	private readonly IFileService _fileService;
	private readonly AsyncLazy<CreatedTaskCollection> _tasks;
	private readonly AsyncLazy<TaughtClass> _taughtClass;
	#endregion

	#region Constructors
	private TaughtSubject(
		IFileService fileService,
		AsyncLazy<TaughtClass> taughtClass,
		AsyncLazy<CreatedTaskCollection> tasks
	)
	{
		_taughtClass = taughtClass;
		_fileService = fileService;
		_tasks = tasks;
	}

	private TaughtSubject(
		IFileService fileService,
		string name,
		AsyncLazy<TaughtClass> taughtClass,
		AsyncLazy<CreatedTaskCollection> tasks
	) : this(fileService: fileService, taughtClass: taughtClass, tasks: tasks)
	{
		Name = name;
	}

	private TaughtSubject(
		IFileService fileService,
		TaughtSubjectResponse response,
		AsyncLazy<TaughtClass> taughtClass,
		AsyncLazy<CreatedTaskCollection> tasks
	) : this(fileService: fileService, taughtClass: taughtClass, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	internal bool TaughtClassIsCreated => _taughtClass.IsValueCreated;
	internal bool TasksAreCreated => _tasks.IsValueCreated;
	#endregion

	#region Records
	internal sealed record TaughtSubjectClass(int Id, string Name);
	internal sealed record TaughtSubjectResponse(int Id, string Name, TaughtSubjectClass Class);
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
		int educationPeriodId = 0,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(
			fileService: fileService,
			response: response, tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory: async () => await CreatedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)),
			taughtClass: new AsyncLazy<TaughtClass>(valueFactory: async () => await TaughtClass.Create(
				client: client,
				subjectId: response.Id,
				classId: response.Class.Id,
				name: response.Class.Name,
				educationPeriodId: educationPeriodId,
				cancellationToken: cancellationToken
			))
		);
	}

	internal static async Task<TaughtSubject> Create(
		ApiClient client,
		IFileService fileService,
		string name,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(
			fileService: fileService,
			name: name,
			tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory: async () => await CreatedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			)),
			taughtClass: new AsyncLazy<TaughtClass>(valueFactory: async () => TaughtClass.Empty)
		);
	}

	internal static TaughtSubject CreateWithoutTasks(
		IFileService fileService,
		string name
	)
	{
		return new TaughtSubject(
			fileService: fileService,
			name: name,
			tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory: async () => CreatedTaskCollection.Empty),
			taughtClass: new AsyncLazy<TaughtClass>(valueFactory: async () => TaughtClass.Empty)
		);
	}

	internal static async Task<TaughtSubject> CreateWithoutTasks(
		IFileService fileService,
		TaughtSubjectResponse response,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(
			fileService: fileService,
			response: response,
			tasks: new AsyncLazy<CreatedTaskCollection>(valueFactory: async () => CreatedTaskCollection.Empty),
			taughtClass: new AsyncLazy<TaughtClass>(valueFactory: async () => await TaughtClass.Create(
				client: fileService.ApiClient,
				subjectId: response.Id,
				classId: response.Class.Id,
				name: response.Class.Name,
				educationPeriodId: educationPeriodId,
				cancellationToken: cancellationToken
			))
		);
	}

	#endregion

	#region Instance
	public async Task<CreatedTaskCollection> GetTasks()
		=> await _tasks;

	public async Task<TaughtClass> GetTaughtClass()
		=> await _taughtClass;

	public IInitTaskBuilder CreateTask()
		=> InitTaskBuilder.Create(fileService: _fileService);

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (CreatedTask createdTask in collection.Where(predicate: task => task.Id == e.TaskId))
				await createdTask.OnCompletedTask(e: e);
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (CreatedTask createdTask in collection.Where(predicate: task => task.Id == e.TaskId))
				await createdTask.OnUncompletedTask(e: e);
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			await foreach (CreatedTask createdTask in collection.Where(predicate: task => task.Id == e.TaskId))
				createdTask.OnCreatedTask(e: e);
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