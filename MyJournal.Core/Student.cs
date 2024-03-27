using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Student : User
{
	private readonly Lazy<StudyingSubjectCollection> _studyingSubjects;
	private readonly HubConnection _studentHubConnection;

	private Student(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		Lazy<ChatCollection> chats,
		Lazy<InterlocutorCollection> interlocutors,
		Lazy<IntendedInterlocutorCollection> intendedInterlocutors,
		Lazy<SessionCollection> sessions,
		Lazy<StudyingSubjectCollection> studyingSubjects
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

	public StudyingSubjectCollection StudyingSubjects => _studyingSubjects.Value;

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
			chats: new Lazy<ChatCollection>(value: await ChatCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			interlocutors: new Lazy<InterlocutorCollection>(value: await InterlocutorCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			intendedInterlocutors: new Lazy<IntendedInterlocutorCollection>(value: await IntendedInterlocutorCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			sessions: new Lazy<SessionCollection>(value: await SessionCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			)),
			studyingSubjects: new Lazy<StudyingSubjectCollection>(value: await StudyingSubjectCollection.Create(
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
			await StudyingSubjects.OnCompletedTask(e: new StudyingSubjectCollection.CompletedTaskEventArgs(taskId: taskId))
		);
		_studentHubConnection.On<int>(methodName: StudentHubMethods.UncompletedTask, handler: async taskId =>
			await StudyingSubjects.OnUncompletedTask(e: new StudyingSubjectCollection.UncompletedTaskEventArgs(taskId: taskId))
		);
		_studentHubConnection.On<int, int>(methodName: StudentHubMethods.TeacherCreatedTask, handler: async (taskId, subjectId) =>
			await StudyingSubjects.OnCreatedTask(e: new StudyingSubjectCollection.CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId))
		);
	}
}