using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Teacher : User
{
	private readonly AsyncLazy<TaughtSubjectCollection> _taughtSubjectCollection;
	private readonly HubConnection _teacherHubConnection;

	private Teacher(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<ChatCollection> chats,
		AsyncLazy<InterlocutorCollection> interlocutors,
		AsyncLazy<IntendedInterlocutorCollection> intendedInterlocutors,
		AsyncLazy<SessionCollection> sessions,
		AsyncLazy<TaughtSubjectCollection> taughtSubjects
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
		_taughtSubjectCollection = taughtSubjects;
		_teacherHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: TeacherHubMethods.HubEndpoint,
			token: client.Token!
		);
	}

	public async Task<TaughtSubjectCollection> GetTaughtSubjects()
		=> await _taughtSubjectCollection;

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
			taughtSubjects: new AsyncLazy<TaughtSubjectCollection>(valueFactory: async () => await TaughtSubjectCollection.Create(
				client: client,
				fileService: fileService,
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
		{ }	// await TaughtSubjects.OnCompletedTask(e: new TaughtSubjectCollection.CompletedTaskEventArgs(taskId: taskId))
		);
		_teacherHubConnection.On<int>(methodName: TeacherHubMethods.StudentUncompletedTask, handler: async taskId =>
		{ }	// await TaughtSubjects.OnUncompletedTask(e: new TaughtSubjectCollection.UncompletedTaskEventArgs(taskId: taskId))
		);
		_teacherHubConnection.On<int, int>(methodName: TeacherHubMethods.CreatedTask, handler: async (taskId, subjectId) =>
		{ }	// await TaughtSubjects.OnCreatedTask(e: new TaughtSubjectCollection.CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId))
		);
	}
}