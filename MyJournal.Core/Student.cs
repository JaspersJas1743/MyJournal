using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Student : User
{
	private readonly AsyncLazy<StudyingSubjectCollection> _studyingSubjects;
	private readonly HubConnection _studentHubConnection;

	private Student(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<ChatCollection> chats,
		AsyncLazy<InterlocutorCollection> interlocutors,
		AsyncLazy<IntendedInterlocutorCollection> intendedInterlocutors,
		AsyncLazy<SessionCollection> sessions,
		AsyncLazy<StudyingSubjectCollection> studyingSubjects
	) : base(
		client: client,
		fileService: fileService,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information,
		chats: chats,
		interlocutors: interlocutors,
		intendedInterlocutors: intendedInterlocutors,
		sessions: sessions
	)
	{
		_studyingSubjects = studyingSubjects;
		_studentHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: StudentHubMethods.HubEndpoint,
			token: client.Token!
		);
	}

	public async Task<StudyingSubjectCollection> GetStudyingSubjects()
		=> await _studyingSubjects;

	private sealed record GetStudentInformationResponse(int ClassId);

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
			chats: new AsyncLazy<ChatCollection>(valueFactory: async () => await ChatCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			interlocutors: new AsyncLazy<InterlocutorCollection>(valueFactory: async () => await InterlocutorCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			intendedInterlocutors: new AsyncLazy<IntendedInterlocutorCollection>(valueFactory: async () => await IntendedInterlocutorCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			sessions: new AsyncLazy<SessionCollection>(valueFactory: async () => await SessionCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			)),
			studyingSubjects: new AsyncLazy<StudyingSubjectCollection>(valueFactory: async () => await StudyingSubjectCollection.Create(
				client: client,
				fileService: fileService,
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
				e: new StudyingSubjectCollection.CompletedTaskEventArgs(taskId: taskId)
			));
		});
		_studentHubConnection.On<int>(methodName: StudentHubMethods.UncompletedTask, handler: async taskId =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnUncompletedTask(
				  e: new StudyingSubjectCollection.UncompletedTaskEventArgs(taskId: taskId)
			));
		});
		_studentHubConnection.On<int, int>(methodName: StudentHubMethods.TeacherCreatedTask, handler: async (taskId, subjectId) =>
		{
			await InvokeIfStudyingSubjectsAreCreated(invocation: async collection => await collection.OnCreatedTask(
				e: new StudyingSubjectCollection.CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId)
			));
		});
	}

	private async Task InvokeIfStudyingSubjectsAreCreated(Func<StudyingSubjectCollection, Task> invocation)
	{
		if (!_studyingSubjects.IsValueCreated)
			return;

		StudyingSubjectCollection studyingSubjects = await GetStudyingSubjects();
		await invocation(arg: studyingSubjects);
	}
}