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

	#region Auxiliary methods
	private async Task<Teacher?> GetTeacher()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Authorized<User> authorizedUser = await service.SignIn(credentials: credentials);
		return authorizedUser.Instance as Teacher;
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
	private async Task CheckTaughtSubject(TaughtSubject subject)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
            Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
        });
        TaughtClass @class = await subject.GetTaughtClass();
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
            Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
        });
    }

	[Test]
	public async Task TeacherGetTaughtSubjects_WithCorrectData_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Все классы"));
		await CheckTaughtSubject(subject: await taughtSubjects.GetByIndex(index: 1));
	}
	#endregion

	#region Tasks
	private async Task CheckTaskWithIdEqualsFive(CreatedTask task)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: task.Id, expression: Is.EqualTo(expected: 5));
            Assert.That(actual: task.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
            Assert.That(actual: task.ClassName, expression: Is.EqualTo(expected: "11 класс"));
            Assert.That(actual: task.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
            Assert.That(actual: task.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
            Assert.That(actual: task.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
            Assert.That(actual: task.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
            Assert.That(actual: task.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
        });
    }

	private async Task CheckTaskWithIdEqualsSeven(CreatedTask task)
	{
        Assert.Multiple(testDelegate: () =>
        {
			Assert.That(actual: task.Id, expression: Is.EqualTo(expected: 7));
			Assert.That(actual: task.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
			Assert.That(actual: task.ClassName, expression: Is.EqualTo(expected: "11 класс"));
			Assert.That(actual: task.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
			Assert.That(actual: task.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
			Assert.That(actual: task.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
			Assert.That(actual: task.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
			Assert.That(actual: task.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
        });
    }

	private async Task CheckAddedTask(CreatedTask task, DateTime release, string text)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: task.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
            Assert.That(actual: task.ClassName, expression: Is.EqualTo(expected: "11 класс"));
            Assert.That(actual: task.ReleasedAt, expression: Is.EqualTo(expected: release));
            Assert.That(actual: task.Content.Text, expression: Is.EqualTo(expected: text));
            Assert.That(actual: task.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
            Assert.That(actual: task.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
            Assert.That(actual: task.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
        });
    }

	[Test]
	public async Task TeacherGetCreatedTasks_AfterAddedTask_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection allTasks = await allSubjects.GetTasks();
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await allTasks.ElementAtAsync(index: 1));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 1);
		CreatedTaskCollection firstTaughtSubjectTasks = await firstTaughtSubject.GetTasks();
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 1));

		DateTime release = DateTime.Today.AddDays(3);
		const string text = "Тестовое задание";
		_ = await allSubjects.CreateTask()
			.SetClass(classId: 11)
			.SetSubject(subjectId: 47)
			.SetDateOfRelease(dateOfRelease: release)
			.SetText(text: text)
			.Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		await CheckTaskWithIdEqualsFive(task: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await allTasks.ElementAtAsync(index: 1));
		await CheckAddedTask(task: await allTasks.ElementAtAsync(index: 2), release: release, text: text);
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 3));
		await CheckTaskWithIdEqualsFive(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 1));
		await CheckAddedTask(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 2), release: release, text: text);
	}

	[Test]
	public async Task TeacherGetCreatedTasks_WithDefaultValues_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection allTasks = await allSubjects.GetTasks();
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await allTasks.ElementAtAsync(index: 1));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 1);
		CreatedTaskCollection firstTaughtSubjectTasks = await firstTaughtSubject.GetTasks();
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 1));
	}

	[Test]
	public async Task TeacherGetCreatedTasks_WithChangeStatusToNotExpired_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
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
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = await taughtSubjects.GetByIndex(index: 0);
		CreatedTaskCollection allTasks = await allSubjects.GetTasks();
		await allTasks.SetCompletionStatus(status: CreatedTaskCollection.TaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await allTasks.ElementAtAsync(index: 1));
		TaughtSubject firstTaughtSubject = await taughtSubjects.GetByIndex(index: 1);
		CreatedTaskCollection firstTaughtSubjectTasks = await firstTaughtSubject.GetTasks();
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(task: await firstTaughtSubjectTasks.ElementAtAsync(index: 1));
	}
	#endregion

	#region Assessments
	private async Task<GradeOfStudent> GetGradeOfFirstStudentIfCorrect(IEnumerable<StudentInTaughtClass> students)
	{
		StudentInTaughtClass firstStudent = students.First();
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: firstStudent.Id, expression: Is.EqualTo(expected: 1));
            Assert.That(actual: firstStudent.Surname, expression: Is.EqualTo(expected: "test"));
            Assert.That(actual: firstStudent.Name, expression: Is.EqualTo(expected: "test"));
            Assert.That(actual: firstStudent.Patronymic, expression: Is.EqualTo(expected: "test"));
        });
        return firstStudent.Grade;
	}

	private async Task<GradeOfStudent> GetGradeOfLastStudentIfCorrect(IEnumerable<StudentInTaughtClass> students)
	{
		StudentInTaughtClass secondStudent = students.Last();
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: secondStudent.Id, expression: Is.EqualTo(expected: 2));
            Assert.That(actual: secondStudent.Surname, expression: Is.EqualTo(expected: "Смирнов"));
            Assert.That(actual: secondStudent.Name, expression: Is.EqualTo(expected: "Алексей"));
            Assert.That(actual: secondStudent.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
        });
        return secondStudent.Grade;
	}

	private async Task CheckGradeOfFirstStudent(GradeOfStudent grade)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
            Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
        });
        Estimation estimation = await grade.SingleAsync();
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: estimation.Id, expression: Is.EqualTo(expected: 5));
            Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "Н"));
            Assert.That(actual: estimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-04-01T12:38:17.183")));
            Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: null));
            Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
            Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Truancy));
        });
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
		Assert.That(actual: estimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: estimation.Assessment, expression: Is.EqualTo(expected: "4"));
		Assert.That(actual: estimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: estimation.Comment, expression: Is.EqualTo(expected: null));
		Assert.That(actual: estimation.Description, expression: Is.EqualTo(expected: "Без комментария"));
		Assert.That(actual: estimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	private async Task CheckGradeOfLastStudent(GradeOfStudent grade)
	{
		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await grade.GetEstimations();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 3));
		await CheckEstimationWithIdEqualsOne(estimation: assessments.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: assessments.ElementAtOrDefault(index: 1));
		await CheckEstimationWithIdEqualsThree(estimation: assessments.ElementAtOrDefault(index: 2));
	}

	private async Task CheckGradeAfterAddition(GradeOfStudent grade)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
            Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
        });
        IEnumerable<Estimation> estimations = await grade.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
		await CheckEstimationWithIdEqualsOne(estimation: estimations.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: estimations.ElementAtOrDefault(index: 1));
		await CheckEstimationWithIdEqualsThree(estimation: estimations.ElementAtOrDefault(index: 2));
		Estimation? fourEstimation = estimations.ElementAtOrDefault(index: 3);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: fourEstimation.Assessment, expression: Is.EqualTo(expected: "3"));
            Assert.That(actual: fourEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
            Assert.That(actual: fourEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
            Assert.That(actual: fourEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
        });
    }

	[Test]
	public async Task TeacherGetAssessments_WithDefaultValue_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = @class.Students;
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		GradeOfStudent firstStudentGrade = await GetGradeOfFirstStudentIfCorrect(students: students);
		await CheckGradeOfFirstStudent(grade: firstStudentGrade);
		GradeOfStudent secondStudentGrade = await GetGradeOfLastStudentIfCorrect(students: students);
		await CheckGradeOfLastStudent(grade: secondStudentGrade);
	}

	[Test]
	public async Task TeacherGetAssessments_AfterAdditionAssessment_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = @class.Students;
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		GradeOfStudent secondStudentGrade = await GetGradeOfLastStudentIfCorrect(students: students);
		await CheckGradeOfLastStudent(grade: secondStudentGrade);

		await secondStudentGrade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		await CheckGradeAfterAddition(grade: secondStudentGrade);
	}

	[Test]
	public async Task TeacherGetAssessments_AfterDeletedAssessment_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
            Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
        });
        TaughtClass @class = await subject.GetTaughtClass();
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
            Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
        });
        IEnumerable<StudentInTaughtClass> students = @class.Students;
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		GradeOfStudent secondStudentGrade = await GetGradeOfLastStudentIfCorrect(students: students);
		await CheckGradeOfLastStudent(grade: secondStudentGrade);

		await secondStudentGrade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		await CheckGradeAfterAddition(grade: secondStudentGrade);

		EstimationOfStudent lastAssessment = await secondStudentGrade.LastAsync();
		await lastAssessment.Delete();
		await Task.Delay(millisecondsDelay: 50);

		await CheckGradeOfLastStudent(grade: secondStudentGrade);
	}

	[Test]
	public async Task TeacherGetAssessments_AfterChangedAssessment_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		TaughtSubject subject = await taughtSubjects.GetByIndex(index: 1);
		Assert.That(actual: subject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: subject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaughtClass @class = await subject.GetTaughtClass();
		Assert.That(actual: @class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: @class.Name, expression: Is.EqualTo(expected: "11 класс"));
		IEnumerable<StudentInTaughtClass> students = @class.Students;
		Assert.That(actual: students.Count(), expression: Is.EqualTo(expected: 2));
		GradeOfStudent secondStudentGrade = await GetGradeOfLastStudentIfCorrect(students: students);
		await CheckGradeOfLastStudent(grade: secondStudentGrade);

		EstimationOfStudent lastAssessment = await secondStudentGrade.LastAsync();
		await lastAssessment.Change().GradeTo(newGradeId: 2).CommentTo(newCommentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "4.67"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await secondStudentGrade.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 3));
		await CheckEstimationWithIdEqualsOne(estimation: estimations.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: estimations.ElementAtOrDefault(index: 1));
		Estimation? thirdEstimation = estimations.ElementAtOrDefault(index: 2);
		Assert.That(actual: thirdEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: thirdEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: thirdEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: thirdEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: thirdEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: thirdEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}
	#endregion

	#region Timetable
	private async Task CheckTimetable(TimetableForTeacher timetable)
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
			Assert.That(actual: timetable.Break, expression: Is.EqualTo(expected: null));
		});
	}

	private async Task PrintTimetable(IEnumerable<TimetableForTeacher> timetable)
	{
		foreach (TimetableForTeacher t in timetable)
		{
			Debug.WriteLine(
				$"timetable:\n" +
				$"\tsubject=[Id={t.Subject.Id},Date={t.Subject.Date},Name={t.Subject.Name},ClassName={t.Subject.ClassName},Number={t.Subject.Number},Start={t.Subject.Start},End={t.Subject.End}]\n" +
				$"\tbreak=[{t.Break?.CountMinutes}]"
			);
		}
	}

	private async Task PrintTimetable(TimetableForTeacherCollection timetable)
	{
		await foreach (KeyValuePair<DateOnly, IEnumerable<TimetableForTeacher>> t in timetable)
		{
			Debug.WriteLine($"date=[{t.Key}]");
			foreach (TimetableForTeacher tt in t.Value)
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
	public async Task TeacherGetTimetableBySubject_WithDefaultValue_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection subjects = await teacher?.GetTaughtSubjects()!;
		TaughtSubject subject = await subjects.SingleAsync(s => s.Id == 47);
		IEnumerable<TimetableForTeacher> timetables = await subject.GetTimetable();
		foreach (TimetableForTeacher timetable in timetables)
			await CheckTimetable(timetable: timetable);
	}

	[Test]
	public async Task TeacherGetTimetableBySubject_AfterChangeTimetable_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection subjects = await teacher?.GetTaughtSubjects()!;
		TaughtSubject subject = await subjects.SingleAsync(s => s.Id == 47);
		await PrintTimetable(timetable: await subject.GetTimetable());

		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder t = @class.CreateTimetable();
		BaseTimetableForDayBuilder day = t.ForDay(dayOfWeekId: 1);
		day.RemoveSubject(item: day.Last());
		await t.Save();

		await Task.Delay(millisecondsDelay: 50);

		await PrintTimetable(timetable: await subject.GetTimetable());
	}

	[Test]
	public async Task StudentGetTimetableByDate_WithDefaultValue_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TimetableForTeacherCollection timetable = await teacher.GetTimetable();
		Assert.That(actual: await timetable.CountAsync(), expression: Is.EqualTo(expected: 7));
	}

	[Test]
	public async Task StudentGetTimetableByDate_AfterChangedTimetable_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		await PrintTimetable(await teacher.GetTimetable());

		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder builder = @class.CreateTimetable();
		BaseTimetableForDayBuilder day = builder.ForDay(dayOfWeekId: 1);
		day.RemoveSubject(item: day.Last());
		await builder.Save();

		await Task.Delay(millisecondsDelay: 50);

		await PrintTimetable(await teacher.GetTimetable());
	}
	#endregion
}