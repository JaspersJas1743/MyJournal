using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubjectInClass : Subject
{
	#region Fields
	private readonly AsyncLazy<TaskAssignedToClassCollection> _tasks;
	private readonly AsyncLazy<List<StudentOfSubjectInClass>> _students;
	#endregion

	#region Constructors
	private StudyingSubjectInClass(
		AsyncLazy<TaskAssignedToClassCollection> tasks,
		AsyncLazy<List<StudentOfSubjectInClass>> students
	)
	{
		_tasks = tasks;
		_students = students;
	}

	private StudyingSubjectInClass(
		string name,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
        AsyncLazy<List<StudentOfSubjectInClass>> students
	) : this(tasks: tasks, students: students) => Name = name;

	private StudyingSubjectInClass(
		StudyingSubjectResponse response,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
        AsyncLazy<List<StudentOfSubjectInClass>> students
	) : this(tasks: tasks, students: students)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
	}
	#endregion

	#region Records
	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
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
	internal static async Task<StudyingSubjectInClass> Create(
		ApiClient client,
		int classId,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(
			response: response,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () =>
				await TaskAssignedToClassCollection.Create(
					client: client,
					subjectId: response.Id,
					classId: classId,
					cancellationToken: cancellationToken
				)
			),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () =>
			{
				IEnumerable<GetStudentsFromClassResponse> students = await client.GetAsync<IEnumerable<GetStudentsFromClassResponse>>(
					apiMethod: ClassControllerMethods.GetStudentsFromClass(classId: classId),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return new List<StudentOfSubjectInClass>(collection: await Task.WhenAll(tasks: students.Select(
					selector: async s => await StudentOfSubjectInClass.Create(
						client: client,
						id: s.Id,
						surname: s.Surname,
						name: s.Name,
						patronymic: s.Patronymic,
						subjectId: response.Id,
						cancellationToken: cancellationToken
					)
				)));
			})
		);
	}

	internal static async Task<StudyingSubjectInClass> Create(
		ApiClient client,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(
			name: name,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () =>
				await TaskAssignedToClassCollection.Create(
					client: client,
					subjectId: 0,
					classId: classId,
					cancellationToken: cancellationToken
				)
			),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () => null)
		);
	}

	internal static async Task<StudyingSubjectInClass> CreateWithoutTasks(
		ApiClient client,
		StudyingSubjectResponse response,
		int classId,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(
			response: response,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () => null),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () =>
			{
				IEnumerable<GetStudentsFromClassResponse> students = await client.GetAsync<IEnumerable<GetStudentsFromClassResponse>>(
					apiMethod: ClassControllerMethods.GetStudentsFromClass(classId: classId),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return new List<StudentOfSubjectInClass>(collection: await Task.WhenAll(tasks: students.Select(
					selector: async s => await StudentOfSubjectInClass.Create(
						client: client,
						id: s.Id,
						surname: s.Surname,
						name: s.Name,
						patronymic: s.Patronymic,
						subjectId: response.Id,
						educationPeriodId: educationPeriodId,
						cancellationToken: cancellationToken
					)
				)));
			})
		);
	}

	internal static StudyingSubjectInClass CreateWithoutTasks(
		string name
	)
	{
		return new StudyingSubjectInClass(
			name: name,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () => null),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () => null)
		);
	}

	#endregion

	#region Instance
	public async Task<TaskAssignedToClassCollection> GetTasks()
		=> await _tasks;

	public async Task<IEnumerable<StudentOfSubjectInClass>> GetStudents()
		=> await _students;

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (TaskAssignedToClass task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnCompletedTask(e: new TaskAssignedToClass.CompletedEventArgs());
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (TaskAssignedToClass task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnUncompletedTask(e: new TaskAssignedToClass.UncompletedEventArgs());
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			await foreach (TaskAssignedToClass task in collection.Where(predicate: t => t.Id == e.TaskId))
				task.OnCreatedTask(e: new TaskAssignedToClass.CreatedEventArgs());
		});

		CreatedTask?.Invoke(e: e);
	}

	private async Task InvokeIfTasksAreCreated(
		Func<TaskAssignedToClassCollection, Task> invocation
	)
	{
		if (!_tasks.IsValueCreated)
			return;

		TaskAssignedToClassCollection collection = await _tasks;
		await invocation(arg: collection);
	}
	#endregion
	#endregion
}