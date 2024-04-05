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

	#region Auxiliary methods
	private async Task<Teacher?> GetTeacher()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		return await service.SignIn(credentials: credentials) as Teacher;
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

	[Test]
	public async Task TeacherGetTaughtSubjectsForPeriod_WithCorrectData_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
		TaughtSubjectCollection taughtSubjects = await teacher.GetTaughtSubjects();
		IEnumerable<EducationPeriod> educationPeriods = await taughtSubjects.GetEducationPeriods();
		EducationPeriod educationPeriod = educationPeriods.Last();
		await taughtSubjects.SetEducationPeriod(period: educationPeriod);
		Assert.That(actual: await taughtSubjects.GetLength(), expression: Is.EqualTo(expected: 1));
		await CheckTaughtSubject(subject: await taughtSubjects.GetByIndex(index: 0));
	}

	[Test]
	public async Task TeacherGetTaughtSubjectsForPeriod_WithSetDefaultPeriod_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
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
			.ForClass(classId: 11)
			.ForSubject(subjectId: 47)
			.AddReleaseDate(dateOfRelease: release)
			.AddText(text: text)
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
        return await firstStudent.GetGrade();
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
        return await secondStudent.GetGrade();
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
	public async Task TeacherGetAssessments_WithChangePeriod_ShouldPassed()
	{
		Teacher? teacher = await GetTeacher();
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
		GradeOfStudent firstStudentGrade = await GetGradeOfFirstStudentIfCorrect(students: students);
		Assert.That(actual: firstStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: firstStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments = await firstStudentGrade.GetEstimations();
		Assert.That(actual: assessments.Count(), expression: Is.EqualTo(expected: 0));
		GradeOfStudent secondStudentGrade = await GetGradeOfLastStudentIfCorrect(students: students);
		Assert.That(actual: secondStudentGrade.AverageAssessment, expression: Is.EqualTo(expected: "-.--"));
		Assert.That(actual: secondStudentGrade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> assessments2 = await secondStudentGrade.GetEstimations();
		Assert.That(actual: assessments2.Count(), expression: Is.EqualTo(expected: 0));
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
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
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
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
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
        IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
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
		IEnumerable<StudentInTaughtClass> students = await @class.GetStudents();
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
}