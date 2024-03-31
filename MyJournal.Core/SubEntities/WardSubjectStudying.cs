using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class WardSubjectStudying : Subject
{
	#region Fields
	private readonly AsyncLazy<TaskAssignedToWardCollection> _tasks;
	#endregion

	#region Constructors
	private WardSubjectStudying(
		AsyncLazy<TaskAssignedToWardCollection> tasks
	)
	{
		_tasks = tasks;
	}

	private WardSubjectStudying(
		string name,
		AsyncLazy<TaskAssignedToWardCollection> tasks
	) : this(tasks: tasks)
	{
		Name = name;
	}

	private WardSubjectStudying(
		StudyingSubjectResponse response,
		AsyncLazy<TaskAssignedToWardCollection> tasks
	) : this(tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
	}
	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	#endregion

	#region Properties
	public bool TasksAreCreated => _tasks.IsValueCreated;
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
	internal static async Task<WardSubjectStudying> Create(
		ApiClient client,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(response: response, tasks: new AsyncLazy<TaskAssignedToWardCollection>(
			valueFactory: async () => await TaskAssignedToWardCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static async Task<WardSubjectStudying> Create(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(name: name, tasks: new AsyncLazy<TaskAssignedToWardCollection>(
			valueFactory: async () => await TaskAssignedToWardCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static WardSubjectStudying CreateWithoutTasks(
		StudyingSubjectResponse response
	) => new WardSubjectStudying(response: response, tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => null));

	internal static WardSubjectStudying CreateWithoutTasks(
		string name
	) => new WardSubjectStudying(name: name, tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => null));
	#endregion

	#region Instance
	public async Task<TaskAssignedToWardCollection> GetTasks()
		=> await _tasks;

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			foreach (TaskAssignedToWard task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnCompletedTask(e: new TaskAssignedToWard.CompletedEventArgs());
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			foreach (TaskAssignedToWard task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnUncompletedTask(e: new TaskAssignedToWard.UncompletedEventArgs());
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			foreach (TaskAssignedToWard task in collection.Where(predicate: t => t.Id == e.TaskId))
				task.OnCreatedTask(e: new TaskAssignedToWard.CreatedEventArgs());
		});

		CreatedTask?.Invoke(e: e);
	}

	private async Task InvokeIfTasksAreCreated(
		Func<TaskAssignedToWardCollection, Task> invocation
	)
	{
		if (!_tasks.IsValueCreated)
			return;

		TaskAssignedToWardCollection collection = await _tasks;
		await invocation(arg: collection);
	}
	#endregion
	#endregion
}