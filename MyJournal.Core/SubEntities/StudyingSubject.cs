using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubject : Subject
{
	#region Fields
	private readonly AsyncLazy<AssignedTaskCollection> _tasks;
	private readonly AsyncLazy<Grade<Estimation>> _grade;
	#endregion

	#region Constructors
	private StudyingSubject(
		AsyncLazy<AssignedTaskCollection> tasks,
		AsyncLazy<Grade<Estimation>> grade
	)
	{
		_tasks = tasks;
		_grade = grade;
	}

	private StudyingSubject(
		string name,
		AsyncLazy<AssignedTaskCollection> tasks,
		AsyncLazy<Grade<Estimation>> grade
	) : this(tasks: tasks, grade: grade)
	{
		Name = name;
	}

	private StudyingSubject(
		StudyingSubjectResponse response,
		AsyncLazy<AssignedTaskCollection> tasks,
		AsyncLazy<Grade<Estimation>> grade
	) : this(tasks: tasks, grade: grade)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
	}
	#endregion

	#region Properties
	internal bool TasksAreCreated => _tasks.IsValueCreated;
	internal bool AssessmentsAreCreated => _grade.IsValueCreated;
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
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(
			response: response,
			tasks: new AsyncLazy<AssignedTaskCollection>(valueFactory: async () => await AssignedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => await Grade<Estimation>.Create(
				client: client,
				periodId: educationPeriodId,
				apiMethod: AssessmentControllerMethods.GetAssessments,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			))
		);
	}

	internal static async Task<StudyingSubject> Create(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(
			name: name,
			tasks: new AsyncLazy<AssignedTaskCollection>(valueFactory: async () => await AssignedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			)),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => Grade<Estimation>.Empty)
		);
	}

	internal static async Task<StudyingSubject> CreateWithoutTasks(
		ApiClient client,
		StudyingSubjectResponse response,
		int periodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(
			response: response,
			tasks: new AsyncLazy<AssignedTaskCollection>(valueFactory: async () => null),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => await Grade<Estimation>.Create(
				client: client,
				periodId: periodId,
				apiMethod: AssessmentControllerMethods.GetAssessments,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			))
		);
	}

	internal static StudyingSubject CreateWithoutTasks(
		string name
	)
	{
		return new StudyingSubject(
			name: name,
			tasks: new AsyncLazy<AssignedTaskCollection>(valueFactory: async () => null),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => null)
		);
	}

	#endregion

	#region Instance
	public async Task<AssignedTaskCollection> GetTasks()
		=> await _tasks;

	public async Task<Grade<Estimation>> GetGrade()
		=> await _grade;

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (AssignedTask task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnCompletedTask(e: new AssignedTask.CompletedEventArgs());
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (AssignedTask task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnUncompletedTask(e: new AssignedTask.UncompletedEventArgs());
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			await foreach (AssignedTask task in collection.Where(predicate: t => t.Id == e.TaskId))
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