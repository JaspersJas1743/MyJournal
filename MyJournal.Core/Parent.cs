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

public sealed class Parent : User
{
	private readonly AsyncLazy<WardStudyingSubjectCollection> _wardSubjectsStudying;
	private readonly HubConnection _parentHubConnection;

	private AsyncLazy<TimetableForWardCollection> _timetable;

	private Parent(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		AsyncLazy<WardStudyingSubjectCollection> wardSubjectsStudying,
		AsyncLazy<TimetableForWardCollection> timetable
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
		_timetable = timetable;
		ClosedCurrentSession += OnClosedCurrentSession;
	}

	~Parent() => ClosedCurrentSession -= OnClosedCurrentSession;

	private async void OnClosedCurrentSession()
		=> await _parentHubConnection.StopAsync();

	public async Task<WardStudyingSubjectCollection> GetWardSubjectsStudying()
		=> await _wardSubjectsStudying;

	public async Task<TimetableForWardCollection> GetTimetable()
		=> await _timetable;

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
			wardSubjectsStudying: new AsyncLazy<WardStudyingSubjectCollection>(valueFactory: async () => await WardStudyingSubjectCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			timetable: new AsyncLazy<TimetableForWardCollection>(valueFactory: async () => await TimetableForWardCollection.Create(
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
				e: new CompletedTaskEventArgs(taskId: taskId)
			));
		});
		_parentHubConnection.On<int>(methodName: ParentHubMethods.WardUncompletedTask, handler: async taskId =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnUncompletedTask(
				e: new UncompletedTaskEventArgs(taskId: taskId)
			));
		});
		_parentHubConnection.On<int, int>(methodName: ParentHubMethods.CreatedTaskToWard, handler: async (taskId, subjectId) =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnCreatedTask(
				e: new CreatedTaskEventArgs(taskId: taskId, subjectId: subjectId, classId: -1)
			));
		});
		_parentHubConnection.On<int, int, int, int>(methodName: ParentHubMethods.CreatedFinalAssessmentToWard, handler: async (assessmentId, studentId, subjectId, periodId) =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnCreatedFinalAssessment(
				e: new CreatedFinalAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId, periodId: periodId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId),
					ApiMethodForFinalSP = AssessmentControllerMethods.GetFinalAssessmentsForWard
				}
			));
		});
		_parentHubConnection.On<int, int, int>(methodName: ParentHubMethods.CreatedAssessmentToWard, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnCreatedAssessment(
				e: new CreatedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_parentHubConnection.On<int, int, int>(methodName: ParentHubMethods.ChangedAssessmentToWard, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnChangedAssessment(
				e: new ChangedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.Get(assessmentId: assessmentId)
				}
			));
		});
		_parentHubConnection.On<int, int, int>(methodName: ParentHubMethods.DeletedAssessmentToWard, handler: async (assessmentId, studentId, subjectId) =>
		{
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnDeletedAssessment(
				e: new DeletedAssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId)
				{
					ApiMethod = AssessmentControllerMethods.GetAverageAssessmentsForWard
				}
			));
		});
		_parentHubConnection.On<IEnumerable<int>>(methodName: ParentHubMethods.ChangedTimetable, handler: async (subjectIds) =>
		{
			ChangedTimetableEventArgs e = new ChangedTimetableEventArgs(classId: -1, subjectIds: subjectIds);
			await InvokeIfWardSubjectStudyingAreCreated(invocation: async collection => await collection.OnChangedTimetable(e: e));
			_timetable = new AsyncLazy<TimetableForWardCollection>(valueFactory: async () => await TimetableForWardCollection.Create(
				client: Client,
				cancellationToken: cancellationToken
			));
			OnChangedTimetable(e: e);
		});
	}

	private async Task InvokeIfWardSubjectStudyingAreCreated(Func<WardStudyingSubjectCollection, Task> invocation)
	{
		if (!_wardSubjectsStudying.IsValueCreated)
			return;

		WardStudyingSubjectCollection studyingSubjects = await GetWardSubjectsStudying();
		await invocation(arg: studyingSubjects);
	}
}