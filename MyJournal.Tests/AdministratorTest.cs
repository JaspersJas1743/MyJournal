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

	[Test]
	public async Task AdministratorGetAssessments_WithDefaultValue_ShouldPassed()
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
		StudyingSubjectInClassCollection subjects = await @class.GetStudyingSubjects();
		StudyingSubjectInClass subject = await subjects.SingleAsync(predicate: s => s.Id == 47);
		IEnumerable<StudentOfSubjectInClass> students = await subject.GetStudents();
		StudentOfSubjectInClass student = students.Single(predicate: s => s.Id == 2);
		Assert.That(actual: student.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: student.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: student.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: student.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await student.GetGrade();
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
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 13));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task AdministratorGetAssessments_WithChangePeriod_ShouldPassed()
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
		StudyingSubjectInClassCollection subjects = await @class.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await subjects.GetEducationPeriods();
		await subjects.SetEducationPeriod(period: educationPeriods.Single(predicate: ep => ep.Id == 8));
		StudyingSubjectInClass subject = await subjects.SingleAsync(predicate: s => s.Id == 47);
		IEnumerable<StudentOfSubjectInClass> students = await subject.GetStudents();
		StudentOfSubjectInClass student = students.Single(predicate: s => s.Id == 2);
		Assert.That(actual: student.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: student.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: student.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: student.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await student.GetGrade();
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await secondStudentGrade.GetAssessments();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task AdministratorGetAssessments_AfterAddedAssessment_ShouldPassed()
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
		StudyingSubjectInClassCollection subjects = await @class.GetStudyingSubjects();
		StudyingSubjectInClass subject = await subjects.SingleAsync(predicate: s => s.Id == 47);
		IEnumerable<StudentOfSubjectInClass> students = await subject.GetStudents();
		StudentOfSubjectInClass student = students.Single(predicate: s => s.Id == 2);
		Assert.That(actual: student.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: student.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: student.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: student.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await student.GetGrade();
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

		await secondStudentGrade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetAssessments();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
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
		Estimation? fourEstimation = estimations.ElementAtOrDefault(index: 3);
		Assert.That(actual: fourEstimation.Assessment, expression: Is.EqualTo(expected: "3"));
		Assert.That(actual: fourEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: fourEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: fourEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task AdministratorGetAssessments_AfterChangedAssessment_ShouldPassed()
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
		StudyingSubjectInClassCollection subjects = await @class.GetStudyingSubjects();
		StudyingSubjectInClass subject = await subjects.SingleAsync(predicate: s => s.Id == 47);
		IEnumerable<StudentOfSubjectInClass> students = await subject.GetStudents();
		StudentOfSubjectInClass student = students.Single(predicate: s => s.Id == 2);
		Assert.That(actual: student.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: student.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: student.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: student.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await student.GetGrade();
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

		EstimationOfStudent lastAssessment = await secondStudentGrade.LastAsync();
		await lastAssessment.Change().GradeTo(newGradeId: 2).CommentTo(newCommentId: 2).Save();

		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.67"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetAssessments();
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
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task AdministratorGetAssessments_AfterDeletedAssessment_ShouldPassed()
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
		StudyingSubjectInClassCollection subjects = await @class.GetStudyingSubjects();
		StudyingSubjectInClass subject = await subjects.SingleAsync(predicate: s => s.Id == 47);
		IEnumerable<StudentOfSubjectInClass> students = await subject.GetStudents();
		StudentOfSubjectInClass student = students.Single(predicate: s => s.Id == 2);
		Assert.That(actual: student.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: student.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: student.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: student.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		GradeOfStudent secondStudentGrade = await student.GetGrade();
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

		await secondStudentGrade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetAssessments();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
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
}