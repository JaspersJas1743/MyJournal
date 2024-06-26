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

	#region Auxiliary methods
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

	private async Task<StudyingSubjectInClassCollection> GetStudyingSubjectInClassCollection(Administrator administrator)
	{
		ClassCollection classes = await administrator.GetClasses();
		Class @class = await classes.GetByIndex(index: 10);
		return await @class.GetStudyingSubjects();
	}
	#endregion

	#region Lessons
	public async Task CheckStudyingSubjectsInClass(StudyingSubjectInClassCollection collection, int startIndex)
	{
		StudyingSubjectInClass firstStudyingSubject = await collection.GetByIndex(index: startIndex);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
            Assert.That(actual: firstStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
            Assert.That(actual: firstStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
            Assert.That(actual: firstStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
            Assert.That(actual: firstStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
            Assert.That(actual: firstStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
        });
        StudyingSubjectInClass secondStudyingSubject = await collection.GetByIndex(index: startIndex + 1);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
            Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
            Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
            Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
            Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
            Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
        });
        StudyingSubjectInClass thirdStudyingSubject = await collection.GetByIndex(index: startIndex + 2);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
            Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
            Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
            Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
            Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
            Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
        });
    }

	[Test]
	public async Task AdministratorGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection studyingSubjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass allSubjects = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: allSubjects.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		await CheckStudyingSubjectsInClass(collection: studyingSubjects, startIndex: 1);
	}
	#endregion

	#region Tasks
	private async Task CheckTaskWithIdEqualsFive(TaskAssignedToClass taskWithIdEqualsFive)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: taskWithIdEqualsFive.Id, expression: Is.EqualTo(expected: 5));
            Assert.That(actual: taskWithIdEqualsFive.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
            Assert.That(actual: taskWithIdEqualsFive.ClassName, expression: Is.EqualTo(expected: "11 класс"));
            Assert.That(actual: taskWithIdEqualsFive.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
            Assert.That(actual: taskWithIdEqualsFive.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
            Assert.That(actual: taskWithIdEqualsFive.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
            Assert.That(actual: taskWithIdEqualsFive.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
            Assert.That(actual: taskWithIdEqualsFive.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
        });
    }

	private async Task CheckTaskWithIdEqualsSix(TaskAssignedToClass taskWithIdEqualsSix)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: taskWithIdEqualsSix.Id, expression: Is.EqualTo(expected: 6));
            Assert.That(actual: taskWithIdEqualsSix.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
            Assert.That(actual: taskWithIdEqualsSix.ClassName, expression: Is.EqualTo(expected: "11 класс"));
            Assert.That(actual: taskWithIdEqualsSix.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
            Assert.That(actual: taskWithIdEqualsSix.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
            Assert.That(actual: taskWithIdEqualsSix.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
            Assert.That(actual: taskWithIdEqualsSix.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
            Assert.That(actual: taskWithIdEqualsSix.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
        });
    }

	private async Task CheckTaskWithIdEqualsSeven(TaskAssignedToClass taskWithIdEqualsSeven)
	{
        Assert.Multiple(testDelegate: () =>
        {
			Assert.That(actual: taskWithIdEqualsSeven.Id, expression: Is.EqualTo(expected: 7));
			Assert.That(actual: taskWithIdEqualsSeven.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
			Assert.That(actual: taskWithIdEqualsSeven.ClassName, expression: Is.EqualTo(expected: "11 класс"));
			Assert.That(actual: taskWithIdEqualsSeven.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
			Assert.That(actual: taskWithIdEqualsSeven.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
			Assert.That(actual: taskWithIdEqualsSeven.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
			Assert.That(actual: taskWithIdEqualsSeven.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
			Assert.That(actual: taskWithIdEqualsSeven.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
        });
    }

	private async Task CheckAllTask(StudyingSubjectInClass subjectInClass)
	{
		TaskAssignedToClassCollection allTasks = await subjectInClass.GetTasks();
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		await CheckTaskWithIdEqualsFive(taskWithIdEqualsFive: await allTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSix(taskWithIdEqualsSix: await allTasks.ElementAtAsync(index: 1));
		await CheckTaskWithIdEqualsSeven(taskWithIdEqualsSeven: await allTasks.ElementAtAsync(index: 2));
	}

	[Test]
	public async Task AdministratorGetTasksAssignedTo11Class_WithDefaultValues_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection studyingSubjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));

		StudyingSubjectInClass firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		await CheckAllTask(firstSubject);

		StudyingSubjectInClass secondSubject = await studyingSubjects.GetByIndex(index: 1);
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		TaskAssignedToClassCollection secondStudyingSubjectTasks = await secondSubject.GetTasks();
		Assert.That(actual: await secondStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 0));

		StudyingSubjectInClass thirdSubject = await studyingSubjects.GetByIndex(index: 2);
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaskAssignedToClassCollection thirdStudyingSubjectTasks = await thirdSubject.GetTasks();
		Assert.That(actual: await thirdStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 2));
		await CheckTaskWithIdEqualsFive(taskWithIdEqualsFive: await thirdStudyingSubjectTasks.ElementAtAsync(index: 0));
		await CheckTaskWithIdEqualsSeven(taskWithIdEqualsSeven: await thirdStudyingSubjectTasks.ElementAtAsync(index: 1));

		StudyingSubjectInClass fourSubject = await studyingSubjects.GetByIndex(index: 3);
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		TaskAssignedToClassCollection fourStudyingSubjectTasks = await fourSubject.GetTasks();
		Assert.That(actual: await fourStudyingSubjectTasks.CountAsync(), expression: Is.EqualTo(expected: 1));
		await CheckTaskWithIdEqualsSix(taskWithIdEqualsSix: await fourStudyingSubjectTasks.ElementAtAsync(index: 0));
	}

	[Test]
	public async Task AdministratorGetAssignedTasksTo11Class_WithChangeStatusToNotExpired_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection studyingSubjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
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
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection studyingSubjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		Assert.That(actual: await studyingSubjects.GetLength(), expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = await studyingSubjects.GetByIndex(index: 0);
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = await firstSubject.GetTasks();
		await allTasks.SetCompletionStatus(status: TaskAssignedToClassCollection.TaskCompletionStatus.Expired);
		await CheckAllTask(subjectInClass: firstSubject);
	}
	#endregion

	#region Assessments
	private async Task<StudentOfSubjectInClass> GetStudentIfCorrect(StudyingSubjectInClassCollection collection)
	{
		StudyingSubjectInClass subject = await collection.SingleAsync(predicate: s => s.Id == 47);
		IEnumerable<StudentOfSubjectInClass> students = await subject.GetStudents();
		StudentOfSubjectInClass student = students.Single(predicate: s => s.Id == 2);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: student.Id, expression: Is.EqualTo(expected: 2));
            Assert.That(actual: student.Surname, expression: Is.EqualTo(expected: "Смирнов"));
            Assert.That(actual: student.Name, expression: Is.EqualTo(expected: "Алексей"));
            Assert.That(actual: student.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
        });
		return student;
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

	private async Task CheckDefaultAssessments(GradeOfStudent grade)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.33"));
            Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
        });
        IEnumerable<Estimation> estimations = await grade.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 3));
		await CheckDefaultEstimations(estimations: estimations);
	}

	private async Task CheckDefaultEstimations(IEnumerable<Estimation> estimations)
	{
		await CheckEstimationWithIdEqualsOne(estimation: estimations.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: estimations.ElementAtOrDefault(index: 1));
		await CheckEstimationWithIdEqualsThree(estimation: estimations.ElementAtOrDefault(index: 2));
	}

	private async Task CheckAssessmentsAfterAddition(GradeOfStudent grade)
	{
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.00"));
            Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
        });
        IEnumerable<Estimation> estimations = await grade.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 4));
		await CheckDefaultEstimations(estimations: estimations);
        Estimation? addedAssessment = estimations.ElementAtOrDefault(index: 3);
        Assert.Multiple(testDelegate: () =>
        {
            Assert.That(actual: addedAssessment.Assessment, expression: Is.EqualTo(expected: "3"));
            Assert.That(actual: addedAssessment.Comment, expression: Is.EqualTo(expected: "КлР"));
            Assert.That(actual: addedAssessment.Description, expression: Is.EqualTo(expected: "Классная работа"));
            Assert.That(actual: addedAssessment.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
        });
    }

	[Test]
	public async Task AdministratorGetAssessments_WithDefaultValue_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection subjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		StudentOfSubjectInClass student = await GetStudentIfCorrect(collection: subjects);
		await CheckDefaultAssessments(grade: await student.GetGrade());
	}

	[Test]
	public async Task AdministratorGetAssessments_AfterAddedAssessment_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection subjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		StudentOfSubjectInClass student = await GetStudentIfCorrect(collection: subjects);
		GradeOfStudent grade = await student.GetGrade();
		await CheckDefaultAssessments(grade: grade);

		await grade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		await CheckAssessmentsAfterAddition(grade: grade);
	}

	[Test]
	public async Task AdministratorGetAssessments_AfterChangedAssessment_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection subjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		StudentOfSubjectInClass student = await GetStudentIfCorrect(collection: subjects);
		GradeOfStudent grade = await student.GetGrade();
		await CheckDefaultAssessments(grade: grade);

		EstimationOfStudent lastAssessment = await grade.LastAsync();
		await lastAssessment.Change().GradeTo(newGradeId: 2).CommentTo(newCommentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		Assert.That(actual: grade.AverageAssessment, expression: Is.EqualTo(expected: "4.67"));
		Assert.That(actual: grade.FinalAssessment, expression: Is.EqualTo(expected: null));
		IEnumerable<Estimation> estimations = await grade.GetEstimations();
		Assert.That(actual: estimations.Count(), expression: Is.EqualTo(expected: 3));
		await CheckEstimationWithIdEqualsOne(estimation: estimations.ElementAtOrDefault(index: 0));
		await CheckEstimationWithIdEqualsTwo(estimation: estimations.ElementAtOrDefault(index: 1));
		Estimation? changedEstimation = estimations.ElementAtOrDefault(index: 2);
		Assert.That(actual: changedEstimation.Id, expression: Is.EqualTo(expected: 66));
		Assert.That(actual: changedEstimation.Assessment, expression: Is.EqualTo(expected: "5"));
		Assert.That(actual: changedEstimation.CreatedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-29T15:42:01.883")));
		Assert.That(actual: changedEstimation.Comment, expression: Is.EqualTo(expected: "КлР"));
		Assert.That(actual: changedEstimation.Description, expression: Is.EqualTo(expected: "Классная работа"));
		Assert.That(actual: changedEstimation.GradeType, expression: Is.EqualTo(expected: GradeTypes.Assessment));
	}

	[Test]
	public async Task AdministratorGetAssessments_AfterDeletedAssessment_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		StudyingSubjectInClassCollection subjects = await GetStudyingSubjectInClassCollection(administrator: administrator);
		StudentOfSubjectInClass student = await GetStudentIfCorrect(collection: subjects);
		GradeOfStudent grade = await student.GetGrade();
		await CheckDefaultAssessments(grade: grade);

		await grade.Add().WithGrade(gradeId: 4).WithCreationDate(creationDate: DateTime.Now).WithComment(commentId: 2).Save();
		await Task.Delay(millisecondsDelay: 50);

		await CheckAssessmentsAfterAddition(grade: grade);

		EstimationOfStudent lastAssessment = await grade.LastAsync();
		await lastAssessment.Delete();
		await Task.Delay(millisecondsDelay: 50);

		await CheckDefaultAssessments(grade: grade);
	}
	#endregion

	#region Timetable
	private readonly SubjectInClassOnTimetable[] _subjects = new SubjectInClassOnTimetable[]
	{
		new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 47, number: 1, name: "Физическая культура", start: TimeSpan.Parse("09:00:00"), end: TimeSpan.Parse("09:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
		new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 47, number: 2, name: "Физическая культура", start: TimeSpan.Parse("10:00:00"), end: TimeSpan.Parse("10:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
		new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 72, number: 3, name: "Проектная деятельность", start: TimeSpan.Parse("11:00:00"), end: TimeSpan.Parse("11:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
		new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 72, number: 4, name: "Проектная деятельность", start: TimeSpan.Parse("12:00:00"), end: TimeSpan.Parse("12:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
		new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 5, name: "Русский язык", start: TimeSpan.Parse("13:00:00"), end: TimeSpan.Parse("13:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
	};

	private async Task CheckTimetable(TimetableForClass timetable, int id, string dayName, int totalHours, IEnumerable<SubjectInClassOnTimetable> subjects)
	{
		Assert.Multiple(testDelegate: () =>
		{
			Assert.That(actual: timetable.DayOfWeek.Id, expression: Is.EqualTo(expected: id));
			Assert.That(actual: timetable.DayOfWeek.Name, expression: Is.EqualTo(expected: dayName));
			Assert.That(actual: timetable.TotalHours, expression: Is.EqualTo(expected: totalHours));
			Assert.That(actual: timetable.Subjects, expression: Is.EquivalentTo(expected: subjects));
		});
	}

	private async Task CheckMonday(TimetableForClass timetable)
		=> await CheckTimetable(timetable: timetable, id: 1, dayName: "Понедельник", totalHours: 6, subjects: _subjects.Append(element: new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 6, name: "Русский язык", start: TimeSpan.Parse("14:00:00"), end: TimeSpan.Parse("14:45:00")), @break: null)));

	private async Task CheckMondayAfterChanged(TimetableForClass timetable)
	{
		await CheckTimetable(timetable: timetable, id: 1, dayName: "Понедельник", totalHours: 5, subjects: new SubjectInClassOnTimetable[]
		{
			new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 47, number: 1, name: "Физическая культура", start: TimeSpan.Parse("09:00:00"), end: TimeSpan.Parse("09:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
			new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 47, number: 2, name: "Физическая культура", start: TimeSpan.Parse("10:00:00"), end: TimeSpan.Parse("10:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
			new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 72, number: 3, name: "Проектная деятельность", start: TimeSpan.Parse("11:00:00"), end: TimeSpan.Parse("11:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
			new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 72, number: 4, name: "Проектная деятельность", start: TimeSpan.Parse("12:00:00"), end: TimeSpan.Parse("12:45:00")), @break: new BreakAfterSubject(countMinutes: 15)),
			new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 5, name: "Русский язык", start: TimeSpan.Parse("13:00:00"), end: TimeSpan.Parse("13:45:00")), @break: null)
		});
	}

	private async Task CheckTuesday(TimetableForClass timetable)
		=> await CheckTimetable(timetable: timetable, id: 2, dayName: "Вторник", totalHours: 6, _subjects.Append(element: new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 6, name: "Русский язык", start: TimeSpan.Parse("14:00:00"), end: TimeSpan.Parse("14:45:00")), @break: null)));

	private async Task CheckWednesday(TimetableForClass timetable)
		=> await CheckTimetable(timetable: timetable, id: 3, dayName: "Среда", totalHours: 6, _subjects.Append(element: new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 6, name: "Русский язык", start: TimeSpan.Parse("14:00:00"), end: TimeSpan.Parse("14:45:00")), @break: null)));

	private async Task CheckThursday(TimetableForClass timetable)
		=> await CheckTimetable(timetable: timetable, id: 4, dayName: "Четверг", totalHours: 6, _subjects.Append(element: new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 6, name: "Русский язык", start: TimeSpan.Parse("14:00:00"), end: TimeSpan.Parse("14:45:00")), @break: null)));

	private async Task CheckFriday(TimetableForClass timetable)
		=> await CheckTimetable(timetable: timetable, id: 5, dayName: "Пятница", totalHours: 6, _subjects.Append(element: new SubjectInClassOnTimetable(subject: new SubjectOnTimetableByDay(id: 37, number: 6, name: "Русский язык", start: TimeSpan.Parse("14:00:00"), end: TimeSpan.Parse("14:45:00")), @break: null)));

	private async Task CheckWorkWeek(IEnumerable<TimetableForClass> timetable)
	{
		await CheckMonday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 1));
		await CheckTuesday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 2));
		await CheckWednesday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 3));
		await CheckThursday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 4));
		await CheckFriday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 5));
	}

	private async Task CheckWorkWeekAfterChanged(IEnumerable<TimetableForClass> timetable)
	{
		await CheckMondayAfterChanged(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 1));
		await CheckTuesday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 2));
		await CheckWednesday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 3));
		await CheckThursday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 4));
		await CheckFriday(timetable: timetable.Single(predicate: t => t.DayOfWeek.Id == 5));
	}

	[Test]
	public async Task AdministratorGetTimetable_WithDefaultValue_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		IEnumerable<TimetableForClass> timetables = await @class.GetTimetable();
		await CheckWorkWeek(timetable: timetables);
	}

	[Test]
	public async Task AdministratorChangeTimetable_WithChangeSubject_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder timetable = @class.CreateTimetable();
		BaseTimetableForDayBuilder day = timetable.ForDay(dayOfWeekId: 1);
		_ = day.First().WithNumber(7).WithStartTime(TimeSpan.Parse("15:00:00")).WithEndTime(TimeSpan.Parse("15:45:00"));
		await timetable.Save();
	}

	[Test]
	public async Task AdministratorChangeTimetable_WithRemoveSubject_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder timetable = @class.CreateTimetable();
		BaseTimetableForDayBuilder day = timetable.ForDay(dayOfWeekId: 1);
		day.RemoveSubject(item: day.Last());
		await timetable.Save();
	}

	[Test]
	public async Task AdministratorChangeTimetable_WithAppendSubject_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);
		ITimetableBuilder timetable = @class.CreateTimetable();
		BaseTimetableForDayBuilder day = timetable.ForDay(dayOfWeekId: 1);
		day.AddSubject().WithNumber(number: 1).WithSubject(subjectId: 47).WithStartTime(time: TimeSpan.Parse("09:00:00")).WithEndTime(time: TimeSpan.Parse("09:45:00"));
		day.AddSubject().WithNumber(number: 2).WithSubject(subjectId: 47).WithStartTime(time: TimeSpan.Parse("10:00:00")).WithEndTime(time: TimeSpan.Parse("10:45:00"));
		await timetable.Save();
	}

	[Test]
	public async Task AdministratorGetTimetable_AfterChangedTimetable_ShouldPassed()
	{
		Administrator? administrator = await GetAdministrator();
		ClassCollection classes = await administrator?.GetClasses()!;
		Class @class = await classes.SingleAsync(c => c.Id == 11);

		ITimetableBuilder timetable = @class.CreateTimetable();
		BaseTimetableForDayBuilder day = timetable.ForDay(dayOfWeekId: 1);
		day.RemoveSubject(item: day.Last());
		await timetable.Save();

		await Task.Delay(millisecondsDelay: 50);

		await CheckWorkWeekAfterChanged(timetable: await @class.GetTimetable());
	}
	#endregion
}