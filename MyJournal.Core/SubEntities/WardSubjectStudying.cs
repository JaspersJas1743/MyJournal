using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class WardSubjectStudying : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<TaskAssignedToWardCollection> _tasks;
	private readonly AsyncLazy<Grade<Estimation>> _grade;

	private AsyncLazy<IEnumerable<TimetableForStudent>> _timetable;
	#endregion

	#region Constructors
	private WardSubjectStudying(
		AsyncLazy<TaskAssignedToWardCollection> tasks,
		AsyncLazy<Grade<Estimation>> grade,
		AsyncLazy<IEnumerable<TimetableForStudent>> timetable,
		ApiClient client
	)
	{
		_tasks = tasks;
		_grade = grade;
		_timetable = timetable;
		_client = client;
	}

	private WardSubjectStudying(
		string name,
		AsyncLazy<TaskAssignedToWardCollection> tasks,
		AsyncLazy<Grade<Estimation>> grade,
		AsyncLazy<IEnumerable<TimetableForStudent>> timetable,
		ApiClient client
	) : this(tasks: tasks, grade: grade, timetable: timetable, client: client)
	{
		Name = name;
	}

	private WardSubjectStudying(
		StudyingSubjectResponse response,
		AsyncLazy<TaskAssignedToWardCollection> tasks,
		AsyncLazy<Grade<Estimation>> grade,
		AsyncLazy<IEnumerable<TimetableForStudent>> timetable,
		ApiClient client
	) : this(tasks: tasks, grade: grade, timetable: timetable, client: client)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
	}
	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	private sealed record GetTimetableWithAssessmentsResponse(SubjectOnTimetable Subject, IEnumerable<EstimationOnTimetable> Estimations, BreakAfterSubject? Break);
	private sealed record GetTimetableBySubjectRequest(int SubjectId);
	#endregion

	#region Properties
	internal bool TasksAreCreated => _tasks.IsValueCreated;
	internal bool GradeIsCreated => _grade.IsValueCreated;
	#endregion

	#region Events
	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;
	public event CreatedTaskHandler CreatedTask;
	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	public event ChangedTimetableHandler ChangedTimetable;
	#endregion

	#region Methods
	#region Static

	private static async Task<AsyncLazy<IEnumerable<TimetableForStudent>>> GetTimetable(
		ApiClient client,
		int subjectId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new AsyncLazy<IEnumerable<TimetableForStudent>>(valueFactory: async () =>
		{
			IEnumerable<GetTimetableWithAssessmentsResponse>? timetable = await client.GetAsync<IEnumerable<GetTimetableWithAssessmentsResponse>, GetTimetableBySubjectRequest>(
				apiMethod: TimetableControllerMethods.GetTimetableBySubjectForParent,
				argQuery: new GetTimetableBySubjectRequest(SubjectId: subjectId),
				cancellationToken: cancellationToken
			);
			return timetable?.Select(selector: t => TimetableForStudent.Create(
				subject: t.Subject,
				estimations: t.Estimations,
				@break: t.Break
			)) ?? Enumerable.Empty<TimetableForStudent>();
		});
	}

	internal static async Task<WardSubjectStudying> Create(
		ApiClient client,
		IFileService fileService,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(
			client: client,
			response: response,
			tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => await TaskAssignedToWardCollection.Create(
				client: client,
				fileService: fileService,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => await Grade<Estimation>.Create(
				client: client,
				apiMethod: AssessmentControllerMethods.GetAssessmentsForWard,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			)),
			timetable: await GetTimetable(client: client, subjectId: response.Id, cancellationToken: cancellationToken)
		);
	}

	internal static async Task<WardSubjectStudying> Create(
		ApiClient client,
		IFileService fileService,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(
			client: client,
			name: name,
			tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => await TaskAssignedToWardCollection.Create(
				client: client,
				fileService: fileService,
				subjectId: 0,
				cancellationToken: cancellationToken
			)),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => Grade<Estimation>.Empty),
			timetable: new AsyncLazy<IEnumerable<TimetableForStudent>>(valueFactory: async () => Enumerable.Empty<TimetableForStudent>())
		);
	}

	internal static async Task<WardSubjectStudying> CreateWithoutTasks(
		ApiClient client,
		StudyingSubjectResponse response,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(
			client: client,
			response: response,
			tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => TaskAssignedToWardCollection.Empty),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => await Grade<Estimation>.Create(
				client: client,
				apiMethod: AssessmentControllerMethods.GetAssessmentsForWard,
				subjectId: response.Id,
				periodId: educationPeriodId,
				cancellationToken: cancellationToken
			)),
			timetable: await GetTimetable(client: client, subjectId: response.Id, cancellationToken: cancellationToken)
		);
	}

	internal static WardSubjectStudying CreateWithoutTasks(
		string name
	)
	{
		return new WardSubjectStudying(
			client: null,
			name: name,
			tasks: new AsyncLazy<TaskAssignedToWardCollection>(valueFactory: async () => TaskAssignedToWardCollection.Empty),
			grade: new AsyncLazy<Grade<Estimation>>(valueFactory: async () => Grade<Estimation>.Empty),
			timetable: new AsyncLazy<IEnumerable<TimetableForStudent>>(valueFactory: async () => Enumerable.Empty<TimetableForStudent>())
		);
	}

	#endregion

	#region Instance
	public async Task<TaskAssignedToWardCollection> GetTasks()
		=> await _tasks;

	public async Task<Grade<Estimation>> GetGrade()
		=> await _grade;

	public async Task<IEnumerable<TimetableForStudent>> GetTimetable()
		=> await _timetable;

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (TaskAssignedToWard task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnCompletedTask(e: e);
		});

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await foreach (TaskAssignedToWard task in collection.Where(predicate: t => t.Id == e.TaskId))
				await task.OnUncompletedTask(e: e);
		});

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfTasksAreCreated(invocation: async collection =>
		{
			await collection.Append(id: e.TaskId);
			await foreach (TaskAssignedToWard task in collection.Where(predicate: t => t.Id == e.TaskId))
				task.OnCreatedTask(e: e);
		});

		CreatedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		await InvokeIfGradeIsCreated(invocation: async grade => await grade.OnCreatedFinalAssessment(e: e));

		CreatedFinalAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		await InvokeIfGradeIsCreated(invocation: async grade => await grade.OnCreatedAssessment(e: e));

		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		await InvokeIfGradeIsCreated(invocation: async grade => await grade.OnChangedAssessment(e: e));

		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		await InvokeIfGradeIsCreated(invocation: async grade => await grade.OnDeletedAssessment(e: e));

		DeletedAssessment?.Invoke(e: e);
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

	private async Task InvokeIfGradeIsCreated(Func<Grade<Estimation>, Task> invocation)
	{
		if (!_grade.IsValueCreated)
			return;

		Grade<Estimation> grade = await _grade;
		await invocation(arg: grade);
	}

	internal async Task OnChangedTimetable(ChangedTimetableEventArgs e)
	{
		_timetable = await GetTimetable(client: _client, subjectId: Id);
		ChangedTimetable?.Invoke(e: e);
	}
	#endregion
	#endregion
}