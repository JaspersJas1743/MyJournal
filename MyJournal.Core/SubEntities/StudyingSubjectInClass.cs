using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubjectInClass : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly int _studyingSubjectId;
	private readonly AsyncLazy<TaskAssignedToClassCollection> _tasks;
	private readonly AsyncLazy<List<StudentOfSubjectInClass>> _students;
	#endregion

	#region Constructors
	private StudyingSubjectInClass(
		ApiClient client,
		int subjectId,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
		AsyncLazy<List<StudentOfSubjectInClass>> students
	)
	{
		_studyingSubjectId = subjectId;
		_client = client;
		_tasks = tasks;
		_students = students;
	}

	private StudyingSubjectInClass(
		ApiClient client,
		string name,
		int subjectId,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
        AsyncLazy<List<StudentOfSubjectInClass>> students
	) : this(
		client: client,
		tasks: tasks,
		students: students,
		subjectId: subjectId
	) => Name = name;

	private StudyingSubjectInClass(
		int subjectId,
		StudyingSubjectResponse response,
		ApiClient client,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
        AsyncLazy<List<StudentOfSubjectInClass>> students
	) : this(
		client: client,
		tasks: tasks,
		students: students,
		subjectId: subjectId
	)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
	}
	#endregion

	#region Records
	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	public sealed record Attendance(int StudentId, bool IsPresent, int? CommentId);
	private sealed record SetAttendanceRequest(int SubjectId, DateTime Datetime, IEnumerable<Attendance> Attendances);
	#endregion

	#region Properties
	internal bool TasksAreCreated => _tasks.IsValueCreated;
	internal bool StudentsAreCreated => _students.IsValueCreated;
	#endregion

	#region Events
	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;
	public event CreatedTaskHandler CreatedTask;
	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	#region Methods
	#region Static
	internal static async Task<StudyingSubjectInClass> Create(
		ApiClient client,
		IFileService fileService,
		int classId,
		int subjectId,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(
			response: response,
			subjectId: subjectId,
			client: client,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () =>
				await TaskAssignedToClassCollection.Create(
					client: client,
					fileService: fileService,
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
		IFileService fileService,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(
			name: name,
			subjectId: -1,
			client: client,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () =>
				await TaskAssignedToClassCollection.Create(
					client: client,
					fileService: fileService,
					subjectId: 0,
					classId: classId,
					cancellationToken: cancellationToken
				)
			),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () => new List<StudentOfSubjectInClass>())
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
			subjectId: response.Id,
			client: client,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () => TaskAssignedToClassCollection.Empty),
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
			client: ApiClient.Empty,
			name: name,
			subjectId: -1,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () => TaskAssignedToClassCollection.Empty),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () => new List<StudentOfSubjectInClass>())
		);
	}

	#endregion

	#region Instance
	public async Task<TaskAssignedToClassCollection> GetTasks()
		=> await _tasks;

	public async Task<IEnumerable<StudentOfSubjectInClass>> GetStudents()
		=> await _students;

	public async Task SetAttendance(
		DateTime date,
		IEnumerable<Attendance> attendance,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PutAsync<SetAttendanceRequest>(
			apiMethod: AssessmentControllerMethods.SetAttendance,
			arg: new SetAttendanceRequest(SubjectId: _studyingSubjectId, Datetime: date, Attendances: attendance),
			cancellationToken: cancellationToken
		);
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (TaskAssignedToClass task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnCompletedTask(e: e);
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (TaskAssignedToClass task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnUncompletedTask(e: e);
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			await foreach (TaskAssignedToClass task in collection.Where(predicate: t => t.Id == e.TaskId))
				task.OnCreatedTask(e: e);
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

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(
			invocation: async subject => await subject.OnCreatedFinalAssessment(e: e),
			studentFilter: student => student.Id == e.StudentId
		);

		CreatedFinalAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(
			invocation: async subject => await subject.OnCreatedAssessment(e: e),
			studentFilter: student => student.Id == e.StudentId
		);

		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(
			invocation: async subject => await subject.OnChangedAssessment(e: e),
			studentFilter: student => student.Id == e.StudentId
		);

		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(
			invocation: async subject => await subject.OnDeletedAssessment(e: e),
			studentFilter: student => student.Id == e.StudentId
		);

		DeletedAssessment?.Invoke(e: e);
	}

	private async Task InvokeIfStudentIsCreated(
		Func<StudentOfSubjectInClass, Task> invocation,
		Func<StudentOfSubjectInClass, bool> studentFilter
	)
	{
		if (!_students.IsValueCreated)
			return;

		IEnumerable<StudentOfSubjectInClass> students = await _students;
		StudentOfSubjectInClass? student = students.SingleOrDefault(predicate: studentFilter);
		if (student is not null)
			await invocation(arg: student);
	}
	#endregion
	#endregion
}