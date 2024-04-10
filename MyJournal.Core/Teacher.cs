using System.Diagnostics;
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

public sealed class Teacher : User
{
	private readonly AsyncLazy<TaughtSubjectCollection> _taughtSubjectCollection;
	private readonly HubConnection _teacherHubConnection;

	private AsyncLazy<TimetableForTeacherCollection> _timetable;

	private Teacher(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<TaughtSubjectCollection> taughtSubjects,
		AsyncLazy<TimetableForTeacherCollection> timetable
	) : base(
		client: client,
		fileService: fileService,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information
	)
	{
		_taughtSubjectCollection = taughtSubjects;
		_teacherHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: TeacherHubMethods.HubEndpoint,
			token: client.Token!
		);
		_timetable = timetable;
	}

	public async Task<TaughtSubjectCollection> GetTaughtSubjects()
		=> await _taughtSubjectCollection;

	public async Task<TimetableForTeacherCollection> GetTimetable()
		=> await _timetable;

	internal static async Task<Teacher> Create(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Teacher teacher = new Teacher(
			client: client,
			fileService: fileService,
			googleAuthenticatorService: googleAuthenticatorService,
			information: information,
			taughtSubjects: new AsyncLazy<TaughtSubjectCollection>(valueFactory: async () => await TaughtSubjectCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			timetable: new AsyncLazy<TimetableForTeacherCollection>(valueFactory: async () => await TimetableForTeacherCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			))
		);
		await teacher.ConnectToUserHub(cancellationToken: cancellationToken);
		await teacher.ConnectToTeacherHub(cancellationToken: cancellationToken);
		return teacher;
	}

	private async Task ConnectToTeacherHub(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _teacherHubConnection.StartAsync(cancellationToken: cancellationToken);
		_teacherHubConnection.On<int>(methodName: TeacherHubMethods.StudentCompletedTask, handler: async taskId =>
		{
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnCompletedTask(
				e: new CompletedTaskEventArgs(taskId: taskId)
			));
		});
		_teacherHubConnection.On<int>(methodName: TeacherHubMethods.StudentUncompletedTask, handler: async taskId =>
		{
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnUncompletedTask(
				e: new UncompletedTaskEventArgs(taskId: taskId)
			));
		});
		_teacherHubConnection.On<int, int>(methodName: TeacherHubMethods.CreatedTask, handler: async (taskId, subjectId) =>
		{
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnCreatedTask(
				e: new CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId, classId: -1)
			));
		});
		_teacherHubConnection.On<int, int, int>(methodName: TeacherHubMethods.CreatedAssessment, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnCreatedAssessment(
				e: new CreatedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_teacherHubConnection.On<int, int, int>(methodName: TeacherHubMethods.ChangedAssessment, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnChangedAssessment(
				e: new ChangedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_teacherHubConnection.On<int, int, int>(methodName: TeacherHubMethods.DeletedAssessment, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnDeletedAssessment(
				e: new DeletedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.GetAverageAssessmentById(studentId: studentId)
				}
			));
		});
		_teacherHubConnection.On<int, IEnumerable<int>>(methodName: TeacherHubMethods.ChangedTimetable, handler: async (classId, subjectIds) =>
		{
			ChangedTimetableEventArgs e = new ChangedTimetableEventArgs(classId: classId, subjectIds: subjectIds);
			await InvokeIfTaughtSubjectsAreCreated(invocation: async collection => await collection.OnChangedTimetable(e: e));
			_timetable = new AsyncLazy<TimetableForTeacherCollection>(valueFactory: async () => await TimetableForTeacherCollection.Create(
				client: Client,
				cancellationToken: cancellationToken
			));
			OnChangedTimetable(e: e);
		});
	}

	private async Task InvokeIfTaughtSubjectsAreCreated(Action<TaughtSubjectCollection> invocation)
	{
		if (!_taughtSubjectCollection.IsValueCreated)
			return;

		TaughtSubjectCollection taughtSubjectCollection = await GetTaughtSubjects();
		invocation(obj: taughtSubjectCollection);
	}
}