using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class TeacherTest
{
	#region SetUp
	private ServiceProvider _serviceProvider = null!;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddGoogleAuthenticator();
		serviceCollection.AddFileService();
		serviceCollection.AddTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>();
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}
	#endregion

	#region Lessons
	[Test]
	public async Task TeacherGetTaughtSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Все классы"));
		TaughtSubject secondTaughtSubject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondTaughtSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: secondTaughtSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTaughtSubject.Class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: secondTaughtSubject.Class.Name, expression: Is.EqualTo(expected: "11 класс"));
	}

	[Test]
	public async Task TeacherGetTaughtSubjectsForPeriod_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await taughtSubjects.GetEducationPeriods();
		EducationPeriod educationPeriod = educationPeriods.Last();
		await taughtSubjects.SetEducationPeriod(period: educationPeriod);
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 1));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstTaughtSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTaughtSubject.Class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: firstTaughtSubject.Class.Name, expression: Is.EqualTo(expected: "11 класс"));
	}

	[Test]
	public async Task TeacherGetStudyingSubjectsForPeriod_WithSetDefaultPeriod_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await taughtSubjects.GetEducationPeriods();
		EducationPeriod lastEducationPeriod = educationPeriods.Last();
		EducationPeriod firstEducationPeriod = educationPeriods.First();
		await taughtSubjects.SetEducationPeriod(period: lastEducationPeriod);
		await taughtSubjects.SetEducationPeriod(period: firstEducationPeriod);
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Все классы"));
		TaughtSubject secondTaughtSubject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondTaughtSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: secondTaughtSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTaughtSubject.Class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: secondTaughtSubject.Class.Name, expression: Is.EqualTo(expected: "11 класс"));
	}
	#endregion

	#region Tasks
	[Test]
	public async Task TeacherGetCreatedTasks_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection allTasks = await allSubjects.GetTasks();
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		CreatedTask firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		CreatedTask secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection firstTaughtSubjectTasks = await firstTaughtSubject.GetTasks();
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		firstTask = await firstTaughtSubjectTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = await firstTaughtSubjectTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}

	[Test]
	public async Task TeacherGetCreatedTasks_WithChangeStatusToNotExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject singleSubject = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection allTasks = await singleSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: CreatedTaskCollection.TaskCompletionStatus.NotExpired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task TeacherGetCreatedTasks_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection allTasks = await allSubjects.GetTasks();
		await allTasks.SetCompletionStatus(status: CreatedTaskCollection.TaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		CreatedTask firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		CreatedTask secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection firstTaughtSubjectTasks = await firstTaughtSubject.GetTasks();
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		firstTask = await firstTaughtSubjectTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = await firstTaughtSubjectTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}
	#endregion
}