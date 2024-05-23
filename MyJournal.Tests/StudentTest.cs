using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Builders.TimetableBuilder;
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

	#region Auxiliary methods
	private async Task<Student?> GetStudent()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Authorized<User> authorizedUser = await service.SignIn(credentials: credentials);
		return authorizedUser.Instance as Student;
	}

	private async Task<Administrator?> GetAdministrator()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Authorized<User> authorizedUser = await service.SignIn(credentials: credentials);
		return authorizedUser.Instance as Administrator;
	}
	#endregion

	#region Lessons
	private async Task CheckStudyingSubjectCollection(StudyingSubjectCollection collection, int startIndex)
	{
		StudyingSubject secondStudyingSubject = await collection.GetByIndex(index: startIndex);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
            Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
            Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
            Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
            Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
            Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
        });
        StudyingSubject thirdStudyingSubject = await collection.GetByIndex(index: startIndex + 1);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
            Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
            Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
            Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
            Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
            Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
        });
        StudyingSubject fourStudyingSubject = await collection.GetByIndex(index: startIndex + 2);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
            Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
            Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
            Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
            Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
            Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
        });
    }

	[Test]
	public async Task StudentGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		int lengthOfStudyingSubjects = await studyingSubjects.GetLength();
		Assert.That(actual: lengthOfStudyingSubjects, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		await CheckStudyingSubjectCollection(collection: studyingSubjects, startIndex: 1);
	}

	[Test]
	public async Task StudentGetStudyingSubjectsForPeriod_WithSetDefaultPeriod_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		await studyingSubjects.SetEducationPeriod(period: educationPeriods.Last());
		await studyingSubjects.SetEducationPeriod(period: educationPeriods.First());
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject firstStudyingSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		await CheckStudyingSubjectCollection(collection: studyingSubjects, startIndex: 1);
	}

	[Test]
	public async Task StudentGetStudyingSubjectsForPeriod_WithCorrectData_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		EducationPeriod period = educationPeriods.Last();
		await studyingSubjects.SetEducationPeriod(period: period);
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 3));
		await CheckStudyingSubjectCollection(collection: studyingSubjects, startIndex: 0);
	}
	#endregion

	#region Tasks
	private async Task CheckTaskWithIdEqualsFive(AssignedTask task)
	{
		Assert.That(actual: task.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: task.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: task.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: task.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: task.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: task.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	private async Task CheckTaskWithIdEqualsSix(AssignedTask task)
	{
		Assert.That(actual: task.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: task.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: task.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: task.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: task.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: task.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	private async Task CheckTaskWithIdEqualsSeven(AssignedTask task)
	{
		Assert.That(actual: task.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: task.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: task.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: task.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: task.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: task.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	private async Task<AssignedTaskCollection> GetAllTask(StudyingSubjectCollection collection)
	{
		Assert.That(actual: await collection.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = await collection.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		return await firstSubject.GetTasks();
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithDefaultValues_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		AssignedTaskCollection allTasks = await GetAllTask(collection: studyingSubjects);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		await CheckTaskWithIdEqualsFive(task: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSix(task: await allTasks.ElementAtAsync(index: 1));
		await CheckTaskWithIdEqualsSeven(task: await allTasks.ElementAtAsync(index: 2));
		StudyingSubject secondSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		AssignedTaskCollection secondStudyingSubjectTasks = await secondSubject.GetTasks();
		Assert.That(actual: await secondStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 0));
		StudyingSubject thirdSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		AssignedTaskCollection thirdStudyingSubjectTasks = await thirdSubject.GetTasks();
		Assert.That(actual: await thirdStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await thirdStudyingSubjectTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await thirdStudyingSubjectTasks.ElementAtAsync(index: 1));
		StudyingSubject fourSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		AssignedTaskCollection fourStudyingSubjectTasks = await fourSubject.GetTasks();
		Assert.That(actual: await fourStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 1));
		await CheckTaskWithIdEqualsSix(task: await fourStudyingSubjectTasks.ElementAtAsync(index: 0));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToUncompleted_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		AssignedTaskCollection allTasks = await GetAllTask(collection: studyingSubjects);
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Uncompleted);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsSix(task: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await allTasks.ElementAtAsync(index: 1));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToCompleted_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		AssignedTaskCollection allTasks = await GetAllTask(collection: studyingSubjects);
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Completed);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 1));
		await CheckTaskWithIdEqualsFive(task: await allTasks.ElementAtAsync(index: 0));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToExpired_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		AssignedTaskCollection allTasks = await GetAllTask(collection: studyingSubjects);
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		await CheckTaskWithIdEqualsFive(await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSix(await allTasks.ElementAtAsync(index: 1));
		await CheckTaskWithIdEqualsSeven(await allTasks.ElementAtAsync(index: 2));
	}
	#endregion

	#region Assessment
	private async Task<Grade<Estimation>> GetGrade(StudyingSubjectCollection collection)
	{
		StudyingSubject physicalEducation = await collection.SingleAsync(predicate: s => s.Id == 47);
		Assert.That(actual: physicalEducation.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		return await physicalEducation.GetGrade();
	}

	private async Task CheckEstimationWithIdEqualsOne(Estimation estimation)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: estimation.Id, expression: Is.EqualTo(expected: 1));
			Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "5"));
			Assert.That(actual: estimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:29:48.727")));
			Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: null));
			Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
			Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		});
	}

	private async Task CheckEstimationWithIdEqualsTwo(Estimation estimation)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: estimation.Id, expression: Is.EqualTo(expected: 2));
			Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "4"));
			Assert.That(actual: estimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T13:30:10.443")));
			Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: null));
			Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
			Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		});
	}

	private async Task CheckEstimationWithIdEqualsThree(Estimation estimation)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: estimation.Id, expression: Is.EqualTo(expected: 66));
			Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "4"));
			Assert.That(actual: estimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
			Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: null));
			Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
			Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		});
	}

	private async Task CheckGrade(Grade<Estimation> grade)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
			Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		});
		IEnumerable<Estimation> assessments = await grade.GetEstimations();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 3));
		await CheckEstimationWithIdEqualsOne(estimation: assessments.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: assessments.ElementAtOrDefault(index: 1));
		await CheckEstimationWithIdEqualsThree(estimation: assessments.ElementAtOrDefault(index: 2));
	}

	private async Task<GradeOfStudent> GetGradeOfLastStudent()
	{
		IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials2 = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Authorized<User> authorizedUser = await service2.SignIn(credentials: credentials2);
		Teacher? teacher = authorizedUser.Instance as Teacher;
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = @class.Students;
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		StudentInTaughtClass lastStudent = students.Last();
		return lastStudent.Grade;
	}

	private async Task CheckAdditionEstimation(Estimation estimation)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "3"));
			Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: "КлР"));
			Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
			Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
		});
	}

	private async Task CheckAssessmentAfterAddition(Grade<Estimation> grade)
	{
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await grade.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
		await CheckEstimationWithIdEqualsOne(estimation: estimations.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: estimations.ElementAtOrDefault(index: 1));
		await CheckEstimationWithIdEqualsThree(estimation: estimations.ElementAtOrDefault(index: 2));
		await CheckAdditionEstimation(estimation: estimations.ElementAtOrDefault(index: 3));
	}

	[Test]
	public async Task StudentGetAssessments_WithDefaultValue_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		Grade<Estimation> grade = await GetGrade(collection: studyingSubjects);
		await CheckGrade(grade: grade);
	}

	[Test]
	public async Task StudentGetAssessments_WithChangePeriod_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await studyingSubjects.GetEducationPeriods();
		await studyingSubjects.SetEducationPeriod(period: educationPeriods.Single(predicate: ep => ep.Id == 8));
		Grade<Estimation> grade = await GetGrade(collection: studyingSubjects);
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetEstimations();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task StudentGetAssessments_AfterAddedAssessment_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		Grade<Estimation> grade = await GetGrade(collection: studyingSubjects);
		await CheckGrade(grade: grade);

		GradeOfStudent gradeOfLastStudent = await GetGradeOfLastStudent();
		await gradeOfLastStudent.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		await CheckAssessmentAfterAddition(grade: grade);
	}

	[Test]
	public async Task StudentGetAssessments_AfterChangedAssessment_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		Grade<Estimation> grade = await GetGrade(collection: studyingSubjects);
		await CheckGrade(grade: grade);

		GradeOfStudent gradeOfLastStudent = await GetGradeOfLastStudent();
		EstimationOfStudent lastAssessment = await gradeOfLastStudent.LastAsync();
		await lastAssessment.Change().GradeTo(newGradeId: 2).CommentTo(newCommentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: gradeOfLastStudent.AverageAssessment, expression: Is.EqualTo(expected: "4.67"));
            Assert.That(actual: gradeOfLastStudent.FinalAssessment, expression: Is.EqualTo(expected: null));
        });
        IEnumerable<Estimation> estimations = await gradeOfLastStudent.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 3));
		await CheckEstimationWithIdEqualsOne(estimation: estimations.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: estimations.ElementAtOrDefault(index: 1));
		Estimation? thirdEstimation = estimations.ElementAtOrDefault(index: 2);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
            Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
            Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
            Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
            Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
            Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
        });
    }

	[Test]
	public async Task StudentGetAssessments_AfterDeletedAssessment_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection studyingSubjects = await student.GetStudyingSubjects();
		Grade<Estimation> grade = await GetGrade(collection: studyingSubjects);
		await CheckGrade(grade: grade);

		GradeOfStudent gradeOfLastStudent = await GetGradeOfLastStudent();
		await gradeOfLastStudent.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		await CheckAssessmentAfterAddition(grade: grade);

		EstimationOfStudent lastAssessment = await gradeOfLastStudent.LastAsync();
		await lastAssessment.Delete();
		await Task.Delay(millisecondsDelay: 50);

		await CheckGrade(grade: grade);
	}
	#endregion

	#region Timetable
	private async Task CheckTimetable(TimetableForStudent timetable)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: timetable.Subject.Id, expression: Is.EqualTo(expected: 47));
			Assert.That(actual: timetable.Subject.Number, expression: Is.AnyOf(1, 2));
			Assert.That(actual: timetable.Subject.ClassName, expression: Is.EqualTo(expected: "11 класс"));
			Assert.That(actual: timetable.Subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
			Assert.That(actual: timetable.Subject.Date, expression: Is.AnyOf(
				new DateOnly(year: 2024, month: 4, day: 8),
				new DateOnly(year: 2024, month: 4, day: 9),
				new DateOnly(year: 2024, month: 4, day: 10),
				new DateOnly(year: 2024, month: 4, day: 11),
				new DateOnly(year: 2024, month: 4, day: 12),
				new DateOnly(year: 2024, month: 4, day: 13),
				new DateOnly(year: 2024, month: 4, day: 14),
				new DateOnly(year: 2024, month: 4, day: 15),
				new DateOnly(year: 2024, month: 4, day: 16)
			));
			Assert.That(actual: timetable.Subject.Start, expression: Is.AnyOf(
				new TimeSpan(hours: 9, minutes: 0, seconds: 0),
				new TimeSpan(hours: 10, minutes: 0, seconds: 0)
			));
			Assert.That(actual: timetable.Subject.End, expression: Is.AnyOf(
				new TimeSpan(hours: 9, minutes: 45, seconds: 0),
				new TimeSpan(hours: 10, minutes: 45, seconds: 0)
			));
			Assert.That(
				actual: timetable.Estimations.Select(selector: e => e.Grade),
				expression: Is.EquivalentTo(expected: Enumerable.Empty<string>())
			);
			Assert.That(actual: timetable.Break, expression: Is.AnyOf(null, new BreakAfterSubject(countMinutes: 15)));
		});
	}

	private async Task PrintTimetable(IEnumerable<TimetableForStudent> timetable)
	{
		foreach (TimetableForStudent t in timetable)
		{
			Debug.WriteLine(
				$"timetable:\n" +
				$"\tsubject=[Id={t.Subject.Id},Date={t.Subject.Date},Name={t.Subject.Name},ClassName={t.Subject.ClassName},Number={t.Subject.Number},Start={t.Subject.Start},End={t.Subject.End}]\n" +
				$"\tbreak=[{t.Break?.CountMinutes}]"
			);
		}
	}

	private async Task PrintTimetable(TimetableForStudentCollection timetable)
	{
		await foreach (KeyValuePair<DateOnly, TimetableForStudent[]> t in timetable)
		{
			Debug.WriteLine($"date=[{t.Key}]");
			foreach (TimetableForStudent tt in t.Value)
			{
				Debug.WriteLine(
					$"timetable:\n" +
					$"\tsubject=[Id={tt.Subject.Id},Date={tt.Subject.Date},Name={tt.Subject.Name},ClassName={tt.Subject.ClassName},Number={tt.Subject.Number},Start={tt.Subject.Start},End={tt.Subject.End}]\n" +
					$"\tbreak=[{tt.Break?.CountMinutes}]"
				);
			}
		}
	}

	[Test]
	public async Task StudentGetTimetableBySubject_WithDefaultValue_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection subjects = await student?.GetStudyingSubjects()!;
		StudyingSubject subject = await subjects.SingleAsync(s => s.Id == 47);
		IEnumerable<TimetableForStudent> timetables = await subject.GetTimetable();
		foreach (TimetableForStudent timetable in timetables)
			await CheckTimetable(timetable: timetable);
	}

	[Test]
	public async Task StudentGetTimetableByDate_WithDefaultValue_ShouldPassed()
	{
		Student? student = await GetStudent();
		TimetableForStudentCollection timetable = await student.GetTimetable();
		Assert.That(actual: await timetable.CountAsync(), expression: Is.EqualTo(expected: 7));
	}

	[Test]
	public async Task TeacherGetTimetableBySubject_AfterChangeTimetable_ShouldPassed()
	{
		Student? student = await GetStudent();
		StudyingSubjectCollection subjects = await student?.GetStudyingSubjects()!;
		StudyingSubject subject = await subjects.SingleAsync(s => s.Id == 47);
		await PrintTimetable(timetable: await subject.GetTimetable());

		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder t = await @class.CreateTimetable();
		BaseTimetableForDayBuilder day = t.ForDay(dayOfWeekId: 1);
		day.RemoveSubject(item: day.Last());
		await t.Save();

		await Task.Delay(millisecondsDelay: 50);

		await PrintTimetable(timetable: await subject.GetTimetable());
	}

	[Test]
	public async Task StudentGetTimetableByDate_AfterChangedTimetable_ShouldPassed()
	{
		Student? student = await GetStudent();
		await PrintTimetable(await student.GetTimetable());

		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder builder = await @class.CreateTimetable();
		BaseTimetableForDayBuilder day = builder.ForDay(dayOfWeekId: 1);
		day.RemoveSubject(item: day.Last());
		await builder.Save();

		await Task.Delay(millisecondsDelay: 50);

		await PrintTimetable(await student.GetTimetable());
	}
	#endregion
}