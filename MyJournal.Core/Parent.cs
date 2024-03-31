using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Parent : User
{
	private readonly AsyncLazy<WardSubjectStudyingCollection> _wardSubjectsStudying;
	private readonly HubConnection _parentHubConnection;

	private Parent(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<WardSubjectStudyingCollection> wardSubjectsStudying
	) : base(
		client: client,
		fileService: fileService,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information
	)
	{
		_wardSubjectsStudying = wardSubjectsStudying;
		_parentHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: ParentHubMethods.HubEndpoint,
			token: client.Token!
		);
	}

	public async Task<WardSubjectStudyingCollection> GetWardSubjectsStudying()
		=> await _wardSubjectsStudying;

	internal static async Task<Parent> Create(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Parent parent = new Parent(
			client: client,
			fileService: fileService,
			googleAuthenticatorService: googleAuthenticatorService,
			information: information,
			wardSubjectsStudying: new AsyncLazy<WardSubjectStudyingCollection>(valueFactory: async () => await WardSubjectStudyingCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			))
		);
		await parent.ConnectToUserHub(cancellationToken: cancellationToken);
		await parent.ConnectToParentHub(cancellationToken: cancellationToken);
		return parent;
	}

	private async Task ConnectToParentHub(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _parentHubConnection.StartAsync(cancellationToken: cancellationToken);
		_parentHubConnection.On<int>(methodName: ParentHubMethods.WardCompletedTask, handler: async taskId =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnCompletedTask(
				e: new WardSubjectStudyingCollection.CompletedTaskEventArgs(taskId: taskId)
			));
		});
		_parentHubConnection.On<int>(methodName: ParentHubMethods.WardUncompletedTask, handler: async taskId =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnUncompletedTask(
				e: new WardSubjectStudyingCollection.UncompletedTaskEventArgs(taskId: taskId)
			));
		});
		_parentHubConnection.On<int, int>(methodName: ParentHubMethods.CreatedTaskToWard, handler: async (taskId, subjectId) =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnCreatedTask(
				e: new WardSubjectStudyingCollection.CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId)
			));
		});
	}

	private async Task InvokeIfWardSubjectStudyingAreCreated(Func<WardSubjectStudyingCollection, Task> invocation)
	{
		if (!_wardSubjectsStudying.IsValueCreated)
			return;

		WardSubjectStudyingCollection studyingSubjects = await GetWardSubjectsStudying();
		await invocation(arg: studyingSubjects);
	}
}