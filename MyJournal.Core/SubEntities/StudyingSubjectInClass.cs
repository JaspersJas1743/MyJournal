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
	private readonly int _classId;
	private readonly AsyncLazy<TaskAssignedToClassCollection> _tasks;
	private AsyncLazy<List<StudentOfSubjectInClass>> _students;
	#endregion

	#region Constructors
	private StudyingSubjectInClass(
		ApiClient client,
		int subjectId,
		int classId,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
		AsyncLazy<List<StudentOfSubjectInClass>> students
	)
	{
		_studyingSubjectId = subjectId;
		_classId = classId;
		_client = client;
		_tasks = tasks;
		_students = students;
	}

	private StudyingSubjectInClass(
		ApiClient client,
		string name,
		int subjectId,
		int classId,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
        AsyncLazy<List<StudentOfSubjectInClass>> students
	) : this(
		client: client,
		tasks: tasks,
		classId: classId,
		students: students,
		subjectId: subjectId
	) => Name = name;

	private StudyingSubjectInClass(
		int subjectId,
		StudyingSubjectResponse response,
		ApiClient client,
		int classId,
		AsyncLazy<TaskAssignedToClassCollection> tasks,
        AsyncLazy<List<StudentOfSubjectInClass>> students
	) : this(
		client: client,
		tasks: tasks,
		classId: classId,
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
	private sealed record GetAssessmentsRequest(int PeriodId, int SubjectId);
	private sealed record Grade(int Id, string Assessment, DateTime CreatedAt, string? Comment, string Description, GradeTypes GradeType);
	private sealed record Student(int StudentId, string Surname, string Name, string? Patronymic);
	private sealed record GetAssessmentsByClassResponse(Student Student, string AverageAssessment, int? FinalAssessment, IEnumerable<Grade> Assessments);
	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	public sealed record Attendance(int StudentId, bool IsPresent, int? CommentId);
	private sealed record SetAttendanceRequest(int SubjectId, DateTime Datetime, IEnumerable<Attendance> Attendances);
	#endregion

	#region Properties
	internal bool TasksAreCreated => _tasks.IsValueCreated;
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
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubjectInClass(
			response: response,
			subjectId: subjectId,
			client: client,
			classId: classId,
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
				IEnumerable<GetAssessmentsByClassResponse> students = await client.GetAsync<IEnumerable<GetAssessmentsByClassResponse>, GetAssessmentsRequest>(
					apiMethod: AssessmentControllerMethods.GetAssessmentsByClass(classId: classId),
					argQuery: new GetAssessmentsRequest(PeriodId: educationPeriodId, SubjectId: subjectId),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return new List<StudentOfSubjectInClass>(collection: students.Select(
					selector: s => StudentOfSubjectInClass.Create(
						client: client,
						id: s.Student.StudentId,
						surname: s.Student.Surname,
						name: s.Student.Name,
						patronymic: s.Student.Patronymic,
						subjectId: response.Id,
						response: new GetAssessmentsByIdResponse(
							AverageAssessment: s.AverageAssessment,
							FinalAssessment: s.FinalAssessment,
							Assessments: s.Assessments.Select(selector: a => new EstimationResponse(
								Id: a.Id,
								Assessment: a.Assessment,
								CreatedAt: a.CreatedAt,
								Comment: a.Comment,
								Description: a.Description,
								GradeType: a.GradeType
							))
						)
					)
				));
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
			classId: classId,
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
			classId: classId,
			tasks: new AsyncLazy<TaskAssignedToClassCollection>(valueFactory: async () => TaskAssignedToClassCollection.Empty),
			students: new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () =>
			{
				IEnumerable<GetAssessmentsByClassResponse> students = await client.GetAsync<IEnumerable<GetAssessmentsByClassResponse>, GetAssessmentsRequest>(
					apiMethod: AssessmentControllerMethods.GetAssessmentsByClass(classId: classId),
					argQuery: new GetAssessmentsRequest(PeriodId: educationPeriodId, SubjectId: response.Id),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return new List<StudentOfSubjectInClass>(collection: students.Select(
					selector: s => StudentOfSubjectInClass.Create(
						client: client,
						id: s.Student.StudentId,
						surname: s.Student.Surname,
						name: s.Student.Name,
						patronymic: s.Student.Patronymic,
						subjectId: response.Id,
						response: new GetAssessmentsByIdResponse(
							AverageAssessment: s.AverageAssessment,
							FinalAssessment: s.FinalAssessment,
							Assessments: s.Assessments.Select(selector: a => new EstimationResponse(
								Id: a.Id,
								Assessment: a.Assessment,
								CreatedAt: a.CreatedAt,
								Comment: a.Comment,
								Description: a.Description,
								GradeType: a.GradeType
							))
						)
					)
				));
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
			classId: -1,
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

	public async Task SetEducationPeriod(
		int educationPeriodId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetAssessmentsByClassResponse> students = await _client.GetAsync<IEnumerable<GetAssessmentsByClassResponse>, GetAssessmentsRequest>(
			apiMethod: AssessmentControllerMethods.GetAssessmentsByClass(classId: _classId),
			argQuery: new GetAssessmentsRequest(PeriodId: educationPeriodId, SubjectId: _studyingSubjectId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_students = new AsyncLazy<List<StudentOfSubjectInClass>>(valueFactory: async () => new List<StudentOfSubjectInClass>(collection: students.Select(
			selector: s => StudentOfSubjectInClass.Create(
				client: _client,
				id: s.Student.StudentId,
				surname: s.Student.Surname,
				name: s.Student.Name,
				patronymic: s.Student.Patronymic,
				subjectId: _studyingSubjectId,
				response: new GetAssessmentsByIdResponse(
					AverageAssessment: s.AverageAssessment,
					FinalAssessment: s.FinalAssessment,
					Assessments: s.Assessments.Select(selector: a => new EstimationResponse(
						Id: a.Id,
						Assessment: a.Assessment,
						CreatedAt: a.CreatedAt,
						Comment: a.Comment,
						Description: a.Description,
						GradeType: a.GradeType
					))
				)
			)
		)));
	}

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