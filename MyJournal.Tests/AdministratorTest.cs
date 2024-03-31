using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class AdministratorTest
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
	public async Task AdministratorGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		StudyingSubjectInClass secondStudyingSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubjectInClass thirdStudyingSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubjectInClass fourStudyingSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}

	[Test]
	public async Task AdministratorGetStudyingSubjectsForPeriod_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		EducationPeriod educationPeriod = educationPeriods.Last();
		await studyingSubjects.SetEducationPeriod(period: educationPeriod);
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 3));
		StudyingSubjectInClass firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: firstStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: firstStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: firstStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: firstStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubjectInClass secondStudyingSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubjectInClass thirdStudyingSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}

	[Test]
	public async Task AdministratorGetStudyingSubjectsForPeriod_WithSetDefaultPeriod_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		EducationPeriod lastEducationPeriod = educationPeriods.Last();
		EducationPeriod firstEducationPeriod = educationPeriods.First();
		await studyingSubjects.SetEducationPeriod(period: lastEducationPeriod);
		await studyingSubjects.SetEducationPeriod(period: firstEducationPeriod);
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		StudyingSubjectInClass secondStudyingSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubjectInClass thirdStudyingSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubjectInClass fourStudyingSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}
	#endregion

	#region Tasks
	[Test]
	public async Task AdministratorGetTasksAssignedTo11Class_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = await firstSubject.GetTasks();
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		TaskAssignedToClass firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		TaskAssignedToClass secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaskAssignedToClass thirdTask = await allTasks.ElementAtAsync(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		StudyingSubjectInClass secondSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		TaskAssignedToClassCollection secondStudyingSubjectTasks = await secondSubject.GetTasks();
		Assert.That(actual: await secondStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 0));
		StudyingSubjectInClass thirdSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaskAssignedToClassCollection thirdStudyingSubjectTasks = await thirdSubject.GetTasks();
		Assert.That(actual: await thirdStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 2));
		firstTask = await thirdStudyingSubjectTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = await thirdStudyingSubjectTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		StudyingSubjectInClass fourSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		TaskAssignedToClassCollection fourStudyingSubjectTasks = await fourSubject.GetTasks();
		Assert.That(actual: await fourStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 1));
		firstTask = await fourStudyingSubjectTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}

	[Test]
	public async Task AdministratorGetAssignedTasksTo11Class_WithChangeStatusToNotExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = await firstSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: TaskAssignedToClassCollection.TaskCompletionStatus.NotExpired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task AdministratorGetAssignedTasksTo11Class_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = await firstSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: TaskAssignedToClassCollection.TaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		TaskAssignedToClass firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		TaskAssignedToClass secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaskAssignedToClass thirdTask = await allTasks.ElementAtAsync(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}
	#endregion
}