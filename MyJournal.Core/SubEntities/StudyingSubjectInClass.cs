using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubjectInClass : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<TaskAssignedToClassCollection> _tasks;
	#endregion

	#region Constructors
	private StudyingSubjectInClass(
		ApiClient client,
		Lazy<TaskAssignedToClassCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private StudyingSubjectInClass(
		ApiClient client,
		string name,
		Lazy<TaskAssignedToClassCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
		IsFirst = true;
	}

	private StudyingSubjectInClass(
		ApiClient client,
		StudyingSubjectResponse response,
		Lazy<TaskAssignedToClassCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
		IsFirst = false;
	}
	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	#endregion

	#region Properties
	public TaskAssignedToClassCollection Tasks => _tasks.Value;
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
	internal static async Task<StudyingSubjectInClass> Create(
		ApiClient client,
		int classId,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(client: client, response: response, tasks: new Lazy<TaskAssignedToClassCollection>(value:
			TaskAssignedToClassCollection.Create(
				client: client,
				subjectId: response.Id,
				classId: classId,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static async Task<StudyingSubjectInClass> Create(
		ApiClient client,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(client: client, name: name, tasks: new Lazy<TaskAssignedToClassCollection>(value:
			TaskAssignedToClassCollection.Create(
				client: client,
				subjectId: 0,
				classId: classId,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static StudyingSubjectInClass CreateWithoutTasks(
		ApiClient client,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	) => new StudyingSubjectInClass(client: client, response: response, tasks: new Lazy<TaskAssignedToClassCollection>(value: null));

	internal static StudyingSubjectInClass CreateWithoutTasks(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	) => new StudyingSubjectInClass(client: client, name: name, tasks: new Lazy<TaskAssignedToClassCollection>(value: null));
	#endregion

	#region Instance
	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		foreach (TaskAssignedToClass task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			await task.OnCompletedTask(e: new TaskAssignedToClass.CompletedEventArgs());

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		foreach (TaskAssignedToClass task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			await task.OnUncompletedTask(e: new TaskAssignedToClass.UncompletedEventArgs());

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await Tasks.Append(id: e.TaskId);
		foreach (TaskAssignedToClass task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			task.OnCreatedTask(e: new TaskAssignedToClass.CreatedEventArgs());

		CreatedTask?.Invoke(e: e);
	}
	#endregion
	#endregion
}