using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubject : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<AssignedTaskCollection> _tasks;
	#endregion

	#region Constructors

	private StudyingSubject(
		ApiClient client,
		Lazy<AssignedTaskCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private StudyingSubject(
		ApiClient client,
		string name,
		Lazy<AssignedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
		IsFirst = true;
	}

	private StudyingSubject(
		ApiClient client,
		StudyingSubjectResponse response,
		Lazy<AssignedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
		IsFirst = false;
	}
	#endregion

	#region Properties
	public AssignedTaskCollection Tasks => _tasks.Value;
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
		return new StudyingSubject(client: client, response: response, tasks: new Lazy<AssignedTaskCollection>(value:
			AssignedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static async Task<StudyingSubject> Create(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(client: client, name: name, tasks: new Lazy<AssignedTaskCollection>(value:
			AssignedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}
	#endregion

	#region Instance
	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		foreach (AssignedTask task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			await task.OnCompletedTask(e: new AssignedTask.CompletedEventArgs());

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		foreach (AssignedTask task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			await task.OnUncompletedTask(e: new AssignedTask.UncompletedEventArgs());

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await Tasks.Append(id: e.TaskId);
		foreach (AssignedTask task in Tasks.Where(predicate: t => t.Id == e.TaskId))
			task.OnCreatedTask(e: new AssignedTask.CreatedEventArgs());

		CreatedTask?.Invoke(e: e);
	}
	#endregion
	#endregion
}