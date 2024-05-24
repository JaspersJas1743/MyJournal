using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Student : User
{
	private readonly AsyncLazy<StudyingSubjectCollection> _studyingSubjects;
	private readonly HubConnection _studentHubConnection;
	private AsyncLazy<TimetableForStudentCollection> _timetable;

	private Student(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<StudyingSubjectCollection> studyingSubjects,
		AsyncLazy<TimetableForStudentCollection> timetable
	) : base(
		client: client,
		fileService: fileService,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information
	)
	{
		_studyingSubjects = studyingSubjects;
		_studentHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: StudentHubMethods.HubEndpoint,
			token: client.Token!
		);
		_timetable = timetable;
		ClosedCurrentSession += OnClosedCurrentSession;
	}

	~Student() => ClosedCurrentSession -= OnClosedCurrentSession;

	private async void OnClosedCurrentSession()
		=> await _studentHubConnection.StopAsync();

	public async Task<StudyingSubjectCollection> GetStudyingSubjects()
		=> await _studyingSubjects;

	public async Task<TimetableForStudentCollection> GetTimetable()
		=> await _timetable;

	internal static async Task<Student> Create(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Student student = new Student(
			client: client,
			fileService: fileService,
			googleAuthenticatorService: googleAuthenticatorService,
			information: information,
			studyingSubjects: new AsyncLazy<StudyingSubjectCollection>(valueFactory: async () => await StudyingSubjectCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			timetable: new AsyncLazy<TimetableForStudentCollection>(valueFactory: async () => await TimetableForStudentCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			))
		);
		await student.ConnectToUserHub(cancellationToken: cancellationToken);
		await student.ConnectToStudentHub(cancellationToken: cancellationToken);
		return student;
	}

	private async Task ConnectToStudentHub(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _studentHubConnection.StartAsync(cancellationToken: cancellationToken);
		_studentHubConnection.On<int>(methodName: StudentHubMethods.CompletedTask, handler: async taskId =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnCompletedTask(
				e: new CompletedTaskEventArgs(taskId: taskId)
			));
		});
		_studentHubConnection.On<int>(methodName: StudentHubMethods.UncompletedTask, handler: async taskId =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnUncompletedTask(
				  e: new UncompletedTaskEventArgs(taskId: taskId)
			));
		});
		_studentHubConnection.On<int, int>(methodName: StudentHubMethods.TeacherCreatedTask, handler: async (taskId, subjectId) =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnCreatedTask(
				e: new CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId, classId: -1)
			));
		});
		_studentHubConnection.On<int, int, int, int>(methodName: StudentHubMethods.TeacherCreatedFinalAssessment, handler: async (assessmentId, studentId, subjectId, periodId) =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnCreatedFinalAssessment(
				e: new CreatedFinalAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId, periodId: periodId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId),
					ApiMethodForFinalSP = AssessmentControllerMethods.GetFinalAssessment
				}
			));
		});
		_studentHubConnection.On<int, int, int>(methodName: StudentHubMethods.TeacherCreatedAssessment, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnCreatedAssessment(
				e: new CreatedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_studentHubConnection.On<int, int, int>(methodName: StudentHubMethods.TeacherChangedAssessment, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnChangedAssessment(
				e: new ChangedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_studentHubConnection.On<int, int, int>(methodName: StudentHubMethods.TeacherDeletedAssessment, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnDeletedAssessment(
				e: new DeletedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.GetAverageAssessment
				}
			));
		});
		_studentHubConnection.On<IEnumerable<int>>(methodName: StudentHubMethods.ChangedTimetable, handler: async (subjectIds) =>
		{
			ChangedTimetableEventArgs e = new ChangedTimetableEventArgs(classId: -1, subjectIds: subjectIds);
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnChangedTimetable(e: e));
			_timetable = new AsyncLazy<TimetableForStudentCollection>(valueFactory: async () => await TimetableForStudentCollection.Create(
				client: Client,
				cancellationToken: cancellationToken
			));
			OnChangedTimetable(e: e);
		});
	}

	private async Task InvokeIfStudyingSubjectsAreCreated(
		Func<StudyingSubjectCollection, Task> invocation
	)
	{
		if (!_studyingSubjects.IsValueCreated)
			return;

		StudyingSubjectCollection studyingSubjects = await GetStudyingSubjects();
		await invocation(arg: studyingSubjects);
	}
}