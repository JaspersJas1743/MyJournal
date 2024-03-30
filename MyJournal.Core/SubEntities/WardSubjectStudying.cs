using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class WardSubjectStudying : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<TaskAssignedToWardCollection> _tasks;
	#endregion

	#region Constructors
	private WardSubjectStudying(
		ApiClient client,
		AsyncLazy<TaskAssignedToWardCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private WardSubjectStudying(
		ApiClient client,
		string name,
		AsyncLazy<TaskAssignedToWardCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
	}

	private WardSubjectStudying(
		ApiClient client,
		StudyingSubjectResponse response,
		AsyncLazy<TaskAssignedToWardCollection> tasks
	) : this(client: client, tasks: tasks)
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
	public async Task<TaskAssignedToWardCollection> GetTasks()
		=> await _tasks;
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
		return new WardSubjectStudying(client: client, response: response, tasks: new AsyncLazy<TaskAssignedToWardCollection>(
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
		return new WardSubjectStudying(client: client, name: name, tasks: new AsyncLazy<TaskAssignedToWardCollection>(
			valueFactory: async () => await TaskAssignedToWardCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			)
		));
	}

	internal static WardSubjectStudying CreateWithoutTasks(
		ApiClient client,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	) => new WardSubjectStudying(client: client, response: response, tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => null));

	internal static WardSubjectStudying CreateWithoutTasks(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	) => new WardSubjectStudying(client: client, name: name, tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => null));
	#endregion

	#region Instance
	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		// foreach (TaskAssignedToWard task in Tasks.Where(predicate: t => t.Id == e.TaskId))
		// 	await task.OnCompletedTask(e: new TaskAssignedToWard.CompletedEventArgs());

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		// foreach (TaskAssignedToWard task in Tasks.Where(predicate: t => t.Id == e.TaskId))
		// 	await task.OnUncompletedTask(e: new TaskAssignedToWard.UncompletedEventArgs());

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		// await Tasks.Append(id: e.TaskId);
		// foreach (TaskAssignedToWard task in Tasks.Where(predicate: t => t.Id == e.TaskId))
		// 	task.OnCreatedTask(e: new TaskAssignedToWard.CreatedEventArgs());

		CreatedTask?.Invoke(e: e);
	}
	#endregion
	#endregion
}