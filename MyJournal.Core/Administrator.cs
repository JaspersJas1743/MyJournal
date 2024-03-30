using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Administrator : User
{
	private readonly AsyncLazy<ClassCollection> _classes;
	private readonly HubConnection _administratorHubConnection;

	private Administrator(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<ChatCollection> chats,
		AsyncLazy<InterlocutorCollection> interlocutors,
		AsyncLazy<IntendedInterlocutorCollection> intendedInterlocutors,
		AsyncLazy<SessionCollection> sessions,
		AsyncLazy<ClassCollection> classes
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
		_classes = classes;
		_administratorHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: AdministratorHubMethod.HubEndpoint,
			token: client.Token!
		);
	}

	public async Task<ClassCollection> GetClasses()
		=> await _classes;

	internal static async Task<Administrator> Create(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Administrator administrator = new Administrator(
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
			classes: new AsyncLazy<ClassCollection>(valueFactory: async () => await ClassCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			))
		);
		await administrator.ConnectToUserHub(cancellationToken: cancellationToken);
		await administrator.ConnectToAdministratorHub(cancellationToken: cancellationToken);
		return administrator;
	}

	private async Task ConnectToAdministratorHub(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _administratorHubConnection.StartAsync(cancellationToken: cancellationToken);
		_administratorHubConnection.On<int>(methodName: AdministratorHubMethod.StudentCompletedTask, handler: async taskId =>
		{}	// await Classes.OnCompletedTask(e: new ClassCollection.CompletedTaskEventArgs(taskId: taskId))
		);
		_administratorHubConnection.On<int>(methodName: AdministratorHubMethod.StudentUncompletedTask, handler: async taskId =>
		{}	// await Classes.OnUncompletedTask(e: new ClassCollection.UncompletedTaskEventArgs(taskId: taskId))
		);
		_administratorHubConnection.On<int, int, int>(methodName: AdministratorHubMethod.CreatedTaskToStudents, handler: async (taskId, subjectId, classId) =>
		{}	// await Classes.OnCreatedTask(e: new ClassCollection.CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId, classId: classId))
		);
	}
}