using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class StudentTest
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
	public async Task StudentGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		StudyingSubject secondStudyingSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubject thirdStudyingSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubject fourStudyingSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}

	[Test]
	public async Task StudentGetStudyingSubjectsForPeriod_WithSetDefaultPeriod_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		EducationPeriod lastPeriod = educationPeriods.Last();
		EducationPeriod firstPeriod = educationPeriods.First();
		await studyingSubjects.SetEducationPeriod(period: lastPeriod);
		await studyingSubjects.SetEducationPeriod(period: firstPeriod);
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		StudyingSubject secondStudyingSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubject thirdStudyingSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubject fourStudyingSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}

	[Test]
	public async Task StudentGetStudyingSubjectsForPeriod_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		EducationPeriod period = educationPeriods.Last();
		await studyingSubjects.SetEducationPeriod(period: period);
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 3));
		StudyingSubject firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: firstStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: firstStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: firstStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: firstStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubject secondStudyingSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubject thirdStudyingSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}
	#endregion

	#region Tasks
	[Test]
	public async Task StudentGetAssignedTasks_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = await firstSubject.GetTasks();
		int lengthOfAllTasks = allTasks.Length;
		Assert.That(actual: lengthOfAllTasks, expression: Is.EqualTo(expected: 3));
		AssignedTask firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask thirdTask = await allTasks.ElementAtAsync(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		StudyingSubject secondSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		AssignedTaskCollection secondStudyingSubjectTasks = await secondSubject.GetTasks();
		Assert.That(actual: await secondStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 0));
		StudyingSubject thirdSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		AssignedTaskCollection thirdStudyingSubjectTasks = await thirdSubject.GetTasks();
		Assert.That(actual: await thirdStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 2));
		firstTask = await thirdStudyingSubjectTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		secondTask = await thirdStudyingSubjectTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		StudyingSubject fourSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		AssignedTaskCollection fourStudyingSubjectTasks = await fourSubject.GetTasks();
		Assert.That(actual: await fourStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 1));
		firstTask = await fourStudyingSubjectTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToUncompleted_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = await firstSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Uncompleted);
		int lengthOfAllTasks = allTasks.Length;
		Assert.That(actual: lengthOfAllTasks, expression: Is.EqualTo(expected: 2));
		AssignedTask firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToCompleted_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = await firstSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Completed);
		int lengthOfAllTasks = allTasks.Length;
		Assert.That(actual: lengthOfAllTasks, expression: Is.EqualTo(expected: 1));
		AssignedTask firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = await firstSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		AssignedTask firstTask = await allTasks.ElementAtAsync(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask secondTask = await allTasks.ElementAtAsync(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask thirdTask = await allTasks.ElementAtAsync(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}
	#endregion

	#region Test
	[Test]
	public async Task StudentGetAssessments_WithDefaultValue_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject physicalEducation = await studyingSubjects.SingleAsync(predicate: s => s.Id == 47);
		Assert.That(actual: physicalEducation.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Grade<Estimation> grade = await physicalEducation.GetGrade();
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetAssessments();
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
		Assert.That(actual: secondEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = assessments.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task StudentGetAssessments_WithChangePeriod_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		await studyingSubjects.SetEducationPeriod(period: educationPeriods.Single(predicate: ep => ep.Id == 8));
		StudyingSubject physicalEducation = await studyingSubjects.SingleAsync(predicate: s => s.Id == 47);
		Assert.That(actual: physicalEducation.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Grade<Estimation> grade = await physicalEducation.GetGrade();
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetAssessments();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task StudentGetAssessments_AfterAddedAssessment_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject physicalEducation = await studyingSubjects.SingleAsync(predicate: s => s.Id == 47);
		Assert.That(actual: physicalEducation.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Grade<Estimation> grade = await physicalEducation.GetGrade();
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetAssessments();
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
		Assert.That(actual: secondEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = assessments.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 13));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));

		IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials2 = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service2.SignIn(credentials: credentials2) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass lastStudent = students.Last();
		GradeOfStudent gradeOfLastStudent = await lastStudent.GetGrade();
		await gradeOfLastStudent.Add(gradeId: 4, dateTime: DateTime.Now, commentId: 2);
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await grade.GetAssessments();
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
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 13));
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
	public async Task StudentGetAssessments_AfterChangedAssessment_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject physicalEducation = await studyingSubjects.SingleAsync(predicate: s => s.Id == 47);
		Assert.That(actual: physicalEducation.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Grade<Estimation> grade = await physicalEducation.GetGrade();
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetAssessments();
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
		Assert.That(actual: secondEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = assessments.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));

		IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials2 = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service2.SignIn(credentials: credentials2) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass lastStudent = students.Last();
		GradeOfStudent gradeOfLastStudent = await lastStudent.GetGrade();
		EstimationOfStudent lastAssessment = await gradeOfLastStudent.LastAsync();
		await lastAssessment.Change().GradeTo(newGradeId: 2).CommentTo(newCommentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: gradeOfLastStudent.AverageAssessment, expression: Is.EqualTo(expected: "4.67"));
		Assert.That(actual: gradeOfLastStudent.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await gradeOfLastStudent.GetAssessments();
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
	public async Task StudentGetAssessments_AfterDeletedAssessment_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject physicalEducation = await studyingSubjects.SingleAsync(predicate: s => s.Id == 47);
		Assert.That(actual: physicalEducation.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Grade<Estimation> grade = await physicalEducation.GetGrade();
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetAssessments();
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
		Assert.That(actual: secondEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: secondEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		Estimation? thirdEstimation = assessments.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 13));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));

		IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials2 = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service2.SignIn(credentials: credentials2) as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass lastStudent = students.Last();
		GradeOfStudent gradeOfLastStudent = await lastStudent.GetGrade();

		await gradeOfLastStudent.Add(gradeId: 4, dateTime: DateTime.Now, commentId: 2);

		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: gradeOfLastStudent.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: gradeOfLastStudent.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await gradeOfLastStudent.GetAssessments();
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
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 13));
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

		EstimationOfStudent lastAssessment = await gradeOfLastStudent.LastAsync();
		await lastAssessment.Delete();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: gradeOfLastStudent.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: gradeOfLastStudent.FinalAssessment, expression: Is.EqualTo(expected: null));
		estimations = await gradeOfLastStudent.GetAssessments();
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
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 13));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: firstEstimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}
	#endregion
}