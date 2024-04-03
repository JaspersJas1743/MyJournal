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

public sealed class Administrator : User
{
	private readonly AsyncLazy<ClassCollection> _classes;
	private readonly HubConnection _administratorHubConnection;

	private Administrator(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<ClassCollection> classes
	) : base(
		client: client,
		fileService: fileService,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information
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
		{
            await InvokeIfClassesAreCreated(invocation: async collection => await collection.OnCompletedTask(
                e: new ClassCollection.CompletedTaskEventArgs(taskId: taskId)
			));
		});
		_administratorHubConnection.On<int>(methodName: AdministratorHubMethod.StudentUncompletedTask, handler: async taskId =>
		{
            await InvokeIfClassesAreCreated(invocation: async collection => await collection.OnUncompletedTask(
                e: new ClassCollection.UncompletedTaskEventArgs(taskId: taskId)
			));
		});
		_administratorHubConnection.On<int, int, int>(methodName: AdministratorHubMethod.CreatedTaskToStudents, handler: async (taskId, subjectId, classId) =>
		{
            await InvokeIfClassesAreCreated(invocation: async collection => await collection.OnCreatedTask(
                e: new ClassCollection.CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId, classId: classId)
			));
		});
		_administratorHubConnection.On<int, int, int>(methodName: AdministratorHubMethod.CreatedAssessmentToStudent, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfClassesAreCreated(invocation: async collection => await collection.OnCreatedAssessment(
				e: new CreatedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_administratorHubConnection.On<int, int, int>(methodName: AdministratorHubMethod.ChangedAssessmentToStudent, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfClassesAreCreated(invocation: async collection => await collection.OnChangedAssessment(
				e: new ChangedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_administratorHubConnection.On<int, int, int>(methodName: AdministratorHubMethod.DeletedAssessmentToStudent, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfClassesAreCreated(invocation: async collection => await collection.OnDeletedAssessment(
				e: new DeletedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.GetAverageAssessmentById(studentId: studentId)
				}
			));
		});
	}

	private async Task InvokeIfClassesAreCreated(
		Func<ClassCollection, Task> invocation
	)
	{
		if (!_classes.IsValueCreated)
			return;

		ClassCollection studyingSubjects = await GetClasses();
		await invocation(arg: studyingSubjects);
	}
}