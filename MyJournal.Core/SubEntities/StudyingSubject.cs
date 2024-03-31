using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubject : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<AssignedTaskCollection> _tasks;
	#endregion

	#region Constructors

	private StudyingSubject(
		ApiClient client,
		AsyncLazy<AssignedTaskCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private StudyingSubject(
		ApiClient client,
		string name,
		AsyncLazy<AssignedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
	}

	private StudyingSubject(
		ApiClient client,
		StudyingSubjectResponse response,
		AsyncLazy<AssignedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
	}
	#endregion

	#region Properties
	internal bool TasksAreCreated => _tasks.IsValueCreated;
	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
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
	internal static async Task<StudyingSubject> Create(
		ApiClient client,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(client: client, response: response, tasks: new AsyncLazy<AssignedTaskCollection>(
			valueFactory: async () => await AssignedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static async Task<StudyingSubject> Create(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(client: client, name: name, tasks: new AsyncLazy<AssignedTaskCollection>(
			valueFactory: async () => await AssignedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static StudyingSubject CreateWithoutTasks(
		ApiClient client,
		StudyingSubjectResponse response
	) => new StudyingSubject(client: client, response: response, tasks: new AsyncLazy<AssignedTaskCollection>(valueFactory: async () => null));

	internal static StudyingSubject CreateWithoutTasks(
		ApiClient client,
		string name
	) => new StudyingSubject(client: client, name: name, tasks: new AsyncLazy<AssignedTaskCollection>(valueFactory: async () => null));
	#endregion

	#region Instance
	public async Task<AssignedTaskCollection> GetTasks()
		=> await _tasks;

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			foreach (AssignedTask task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnCompletedTask(e: new AssignedTask.CompletedEventArgs());
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			foreach (AssignedTask task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnUncompletedTask(e: new AssignedTask.UncompletedEventArgs());
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			foreach (AssignedTask task in collection.Where(predicate: t => t.Id == e.TaskId))
				task.OnCreatedTask(e: new AssignedTask.CreatedEventArgs());
		});

		CreatedTask?.Invoke(e: e);
	}

	private async Task InvokeIfTasksAreCreated(Func<AssignedTaskCollection, Task> invocation)
	{
		if (!_tasks.IsValueCreated)
			return;

		AssignedTaskCollection collection = await _tasks;
		await invocation(arg: collection);
	}
	#endregion
	#endregion
}