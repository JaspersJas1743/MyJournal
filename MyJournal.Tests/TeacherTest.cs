using System.Diagnostics;
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
		TaughtClass @class = await secondTaughtSubject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
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
		Debug.WriteLine($"{educationPeriod.Id}: {educationPeriod.Name} started {educationPeriod.StartDate} and ended {educationPeriod.EndDate}");
		await taughtSubjects.SetEducationPeriod(period: educationPeriod);
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 1));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstTaughtSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await firstTaughtSubject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
	}

	[Test]
	public async Task TeacherGetTaughtSubjectsForPeriod_WithSetDefaultPeriod_ShouldPassed()
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
		Assert.That(actual: firstTaughtSubject.Id, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Все классы"));
		TaughtSubject secondTaughtSubject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondTaughtSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: secondTaughtSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await secondTaughtSubject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
	}
	#endregion

	#region Tasks
	[Test]
	public async Task TeacherGetCreatedTasks_AfterAddedTask_ShouldPassed()
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

		DateTime release = DateTime.Today.AddDays(3);
		const string text = "Тестовое задание";
		_ = await allSubjects.CreateTask()
			.ForClass(classId: 11)
			.ForSubject(subjectId: 47)
			.AddReleaseDate(dateOfRelease: release)
			.AddText(text: text)
			.Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 3));
		CreatedTask thirdTask = await allTasks.ElementAtAsync(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 22));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: release));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: text));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
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
		thirdTask = await allTasks.ElementAtAsync(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 22));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: release));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: text));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}

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

	#region Assessments
	[Test]
	public async Task TeacherGetAssessments_WithChangePeriod_ShouldPassed()
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
		await taughtSubjects.SetEducationPeriod(period: educationPeriods.Single(predicate: ep => ep.Id == 8));
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 0);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass firstStudent = students.First();
		Assert.That(actual: firstStudent.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstStudent.Surname, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: firstStudent.Name, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: firstStudent.Patronymic, expression: Is.EqualTo(expected: "test"));
		GradeOfStudent firstStudentGrade = await firstStudent.GetGrade();
		Assert.That(actual: firstStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: firstStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await firstStudentGrade.GetAssessments();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 0));
		StudentInTaughtClass secondStudent = students.Last();
		Assert.That(actual: secondStudent.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudent.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: secondStudent.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: secondStudent.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await secondStudent.GetGrade();
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments2 = await secondStudentGrade.GetAssessments();
		Assert.That(actual: assessments2.Count(), expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task TeacherGetAssessments_WithDefaultValue_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass firstStudent = students.First();
		Assert.That(actual: firstStudent.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstStudent.Surname, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: firstStudent.Name, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: firstStudent.Patronymic, expression: Is.EqualTo(expected: "test"));
		GradeOfStudent firstStudentGrade = await firstStudent.GetGrade();
		Assert.That(actual: firstStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: firstStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		Estimation estimation = await firstStudentGrade.SingleAsync();
		Assert.That(actual: estimation.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "Н"));
		Assert.That(actual: estimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-04-01T12:38:17.183")));
		Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Truancy));
		StudentInTaughtClass secondStudent = students.Last();
		Assert.That(actual: secondStudent.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudent.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: secondStudent.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: secondStudent.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await secondStudent.GetGrade();
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await secondStudentGrade.GetAssessments();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 3));
		Estimation? firstEstimation = assessments.ElementAtOrDefault(index: 0);
		Assert.That(actual: firstEstimation.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: firstEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:29:48.727")));
		Assert.That(actual: firstEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: firstEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? secondEstimation = assessments.ElementAtOrDefault(index: 1);
		Assert.That(actual: secondEstimation.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: secondEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:30:10.443")));
		Assert.That(actual: secondEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = assessments.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task TeacherGetAssessments_AfterAdditionAssessment_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass secondStudent = students.Last();
		Assert.That(actual: secondStudent.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudent.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: secondStudent.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: secondStudent.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await secondStudent.GetGrade();
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));

		await secondStudentGrade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetAssessments();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
		Estimation? firstEstimation = estimations.ElementAtOrDefault(index: 0);
		Assert.That(actual: firstEstimation.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: firstEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:29:48.727")));
		Assert.That(actual: firstEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: firstEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? secondEstimation = estimations.ElementAtOrDefault(index: 1);
		Assert.That(actual: secondEstimation.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: secondEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:30:10.443")));
		Assert.That(actual: secondEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = estimations.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? fourEstimation = estimations.ElementAtOrDefault(index: 3);
		Assert.That(actual: fourEstimation.Assessment, expression: Is.EqualTo(expected: "3"));
		Assert.That(actual: fourEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: fourEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: fourEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task TeacherGetAssessments_AfterDeletedAssessment_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass secondStudent = students.Last();
		Assert.That(actual: secondStudent.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudent.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: secondStudent.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: secondStudent.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await secondStudent.GetGrade();
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));

		await secondStudentGrade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetAssessments();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
		Estimation? firstEstimation = estimations.ElementAtOrDefault(index: 0);
		Assert.That(actual: firstEstimation.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: firstEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:29:48.727")));
		Assert.That(actual: firstEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: firstEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? secondEstimation = estimations.ElementAtOrDefault(index: 1);
		Assert.That(actual: secondEstimation.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: secondEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:30:10.443")));
		Assert.That(actual: secondEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = estimations.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? fourEstimation = estimations.ElementAtOrDefault(index: 3);
		Assert.That(actual: fourEstimation.Assessment, expression: Is.EqualTo(expected: "3"));
		Assert.That(actual: fourEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: fourEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: fourEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));

		EstimationOfStudent lastAssessment = await secondStudentGrade.LastAsync();
		await lastAssessment.Delete();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		estimations = await secondStudentGrade.GetAssessments();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 3));
		firstEstimation = estimations.ElementAtOrDefault(index: 0);
		Assert.That(actual: firstEstimation.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: firstEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:29:48.727")));
		Assert.That(actual: firstEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: firstEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		secondEstimation = estimations.ElementAtOrDefault(index: 1);
		Assert.That(actual: secondEstimation.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: secondEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:30:10.443")));
		Assert.That(actual: secondEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		thirdEstimation = estimations.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task TeacherGetAssessments_AfterChangedAssessment_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass secondStudent = students.Last();
		Assert.That(actual: secondStudent.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudent.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: secondStudent.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: secondStudent.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await secondStudent.GetGrade();
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));

		EstimationOfStudent lastAssessment = await secondStudentGrade.LastAsync();
		await lastAssessment.Change().GradeTo(newGradeId: 2).CommentTo(newCommentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.67"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetAssessments();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 3));
		Estimation? firstEstimation = estimations.ElementAtOrDefault(index: 0);
		Assert.That(actual: firstEstimation.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: firstEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:29:48.727")));
		Assert.That(actual: firstEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: firstEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? secondEstimation = estimations.ElementAtOrDefault(index: 1);
		Assert.That(actual: secondEstimation.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: secondEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:30:10.443")));
		Assert.That(actual: secondEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = estimations.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}
	#endregion
}