using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.RegistrationCodeGenerator;
using MyJournal.API.Assets.Utilities;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/administrator")]
public sealed class AdministratorController(
	MyJournalContext context,
	IRegistrationCodeGeneratorService registrationCodeGeneratorService
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	[Validator<AddTaughtSubjectForTeacherRequestValidator>]
	public sealed record AddTaughtSubjectForTeacherRequest(int TeacherId, int ClassId, int SubjectId);

	[Validator<CreateTeacherRequestValidator>]
	public sealed record CreateTeacherRequest(string Surname, string Name, string? Patronymic, int ClassId, int SubjectId);

	[Validator<CreateStudentRequestValidator>]
	public sealed record CreateStudentRequest(string Surname, string Name, string? Patronymic, int ClassId);

	[Validator<CreateParentRequestValidator>]
	public sealed record CreateParentRequest(string Surname, string Name, string? Patronymic, int ChildrenId);

	[Validator<CreateAdministratorRequestValidator>]
	public sealed record CreateAdministratorRequest(string Surname, string Name, string? Patronymic);

	[Validator<GetStudentsRequestValidator>]
	public sealed record GetStudentsRequest(bool IsFiltered, string? Filter);

	[Validator<GetTeachersRequestValidator>]
	public sealed record GetTeachersRequest(bool IsFiltered, string? Filter);

	public sealed record Classroom(int Id, string Name);
	public sealed record GetStudentsResponse(int Id, string Surname, string Name, string? Patronymic, Classroom Classroom);

	public sealed record TaughtSubject(int SubjectId, string Subject);
	public sealed record TaughtSubjects(TaughtSubject Subject, IEnumerable<Classroom> Classes);
	public sealed record GetTeachersResponse(int Id, string Surname, string Name, string? Patronymic, IEnumerable<TaughtSubjects> TaughtSubjects);
	public sealed record GetEducationPeriodsInClassResponse(int Id, string Name, DateOnly StartDate, DateOnly EndDate);
	public sealed record CreateUserResponse(string RegistrationCode);

	private async Task<UserRole> GetRole(UserRoles role, CancellationToken cancellationToken)
	{
		UserRole teacherRole = await _context.UserRoles.AsNoTracking()
			.FirstAsync(predicate: ur => ur.Role == role, cancellationToken: cancellationToken);
		return teacherRole;
	}

	#region GET
	/// <summary>
	/// [Администратор] Получение списка учебных периодов для класса
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/periods/education/class/{classId:int}/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список учебных периодов подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "periods/education/class/{classId:int}/get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetEducationPeriodsInClassResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetEducationPeriodsInClassResponse>>> GetEducationPeriods(
		[FromRoute] int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<StudentController.GetStudentEducationPeriodsResponse> educationPeriods = _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == classId)
			.SelectMany(selector: c => c.EducationPeriodForClasses)
			.Select(selector: epfc => new StudentController.GetStudentEducationPeriodsResponse(
				epfc.EducationPeriod.Id,
				epfc.EducationPeriod.Period,
				epfc.EducationPeriod.StartDate,
				epfc.EducationPeriod.EndDate
			));

		return Ok(value: educationPeriods);
	}

	/// <summary>
	/// [Администратор] Получение списка всех учеников с фильтрацией
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/students/all/get?IsFiltered=true&Filter=%D0%A1%D0%BC%D0%B8
	///
	/// Параметры:
	///
	///	IsFiltered - логическое значение, отвечающее за наличие фильтрации данных по заданному критерию
	///	Filter - критерий, по которому будет проходить отбор потенциальных собеседников
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список всех обучающихся в учебном заведении</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "students/all/get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudentsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudentsResponse>>> GetStudents(
		[FromQuery] GetStudentsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<Student> students = _context.Students.AsNoTracking()
			.OrderBy(keySelector: s => s.User.Surname)
			.ThenBy(keySelector: s => s.User.Name)
			.ThenBy(keySelector: s => s.User.Patronymic)
			.ThenBy(keySelector: s => s.ClassId);

		if (request.IsFiltered)
		{
			students = students.Where(predicate: s => EF.Functions.Like(
				s.User.Surname + ' ' + s.User.Name + ' ' + s.User.Patronymic,
				request.Filter + '%'
			));
		}

		return Ok(value: students.Select(selector: s => new GetStudentsResponse(
			s.Id,
			s.User.Surname,
			s.User.Name,
			s.User.Patronymic,
			new Classroom(
				s.ClassId,
				s.Class.Name
			)
		)));
	}

	/// <summary>
	/// [Администратор] Получение списка всех преподавателей с фильтрацией
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/teachers/all/get?IsFiltered=true&Filter=%D0%A1%D0%BC%D0%B8
	///
	/// Параметры:
	///
	///	IsFiltered - логическое значение, отвечающее за наличие фильтрации данных по заданному критерию
	///	Filter - критерий, по которому будет проходить отбор потенциальных собеседников
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список всех преподавателей в учебном заведении</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "teachers/all/get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudentsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudentsResponse>>> GetTeachers(
		[FromQuery] GetTeachersRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<Teacher> teachers = _context.Teachers.AsNoTracking()
			.OrderBy(keySelector: s => s.User.Surname)
			.ThenBy(keySelector: s => s.User.Name)
			.ThenBy(keySelector: s => s.User.Patronymic);

		if (request.IsFiltered)
		{
			teachers = teachers.Where(predicate: s => EF.Functions.Like(
				s.User.Surname + ' ' + s.User.Name + ' ' + s.User.Patronymic,
				request.Filter + '%'
			));
		}

		return Ok(value: teachers.Select(selector: t => new GetTeachersResponse(
			t.Id,
			t.User.Surname,
			t.User.Name,
			t.User.Patronymic,
			t.TeachersLessons.Select(tl => new TaughtSubjects(
				new TaughtSubject(tl.LessonId, tl.Lesson.Name),
				tl.Classes.Select(c => new Classroom(c.Id, c.Name))
			))
		)));
	}
	#endregion

	#region POST
	/// <summary>
	/// [Администратор] Добавление нового преподавателя
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/teacher/create
	///	{
	///		"Surname": "Фамилия",
	///		"Name": "Имя",
	///		"Patronymic": "Отчество",
	///		"ClassId": 1,
	///		"SubjectId": 1
	///	}
	///
	/// Параметры:
	///
	///	Surname - фамилия нового пользователя
	///	Name - имя нового пользователя
	///	Patronymic - отчество нового пользователя
	///	ClassId - идентификатор учебного класса для обучения
	///	SubjectId - идентификатор преподаваемой дисциплины
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Регистрационный код добавленного преподавателя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpPost(template: "teacher/create")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CreateUserResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<CreateUserResponse>> CreateTeacher(
		[FromBody] CreateTeacherRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string registrationCode = registrationCodeGeneratorService.Generate();
		UserRole teacherRole = await GetRole(role: UserRoles.Teacher, cancellationToken: cancellationToken);

		UserActivityStatus activityStatus = await FindUserActivityStatus(
			activityStatus: UserActivityStatuses.Offline,
			cancellationToken: cancellationToken
		);

		IQueryable<Class> @class = _context.Classes.AsNoTracking().Where(
			predicate: c => c.Id == request.ClassId
		);
		if (!@class.Any())
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор класса.");

		try
		{
			User userPart = new User()
			{
				Surname = request.Surname,
				Name = request.Name,
				Patronymic = request.Patronymic,
				UserActivityStatusId = activityStatus.Id,
				RegistrationCode = registrationCode,
				UserRoleId = teacherRole.Id
			};
			await _context.Users.AddAsync(entity: userPart, cancellationToken: cancellationToken);

			Teacher teacherPart = new Teacher() { User = userPart };
			await _context.Teachers.AddAsync(entity: teacherPart, cancellationToken: cancellationToken);

			TeachersLesson teacherLesson = new TeachersLesson()
			{
				Teacher = teacherPart, LessonId = request.SubjectId,
			};
			await _context.TeachersLessons.AddAsync(entity: teacherLesson, cancellationToken: cancellationToken);
			await _context.SaveChangesAsync(cancellationToken: cancellationToken);

			teacherLesson.Classes = new List<Class>(@class);
			await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		}
		catch (Exception _)
		{
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Произошла ошибка при сохранении.");
		}

		return Ok(value: new CreateUserResponse(RegistrationCode: registrationCode));
	}

	/// <summary>
	/// [Администратор] Добавление нового обучающегося
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/student/create
	///	{
	///		"Surname": "Фамилия",
	///		"Name": "Имя",
	///		"Patronymic": "Отчество",
	///		"ClassId": 1
	///	}
	///
	/// Параметры:
	///
	///	Surname - фамилия нового пользователя
	///	Name - имя нового пользователя
	///	Patronymic - отчество нового пользователя
	///	ClassId - идентификатор учебного класса обучающегося
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Регистрационный код пользователя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpPost(template: "student/create")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CreateUserResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<CreateUserResponse>> CreateStudent(
		[FromBody] CreateStudentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string registrationCode = registrationCodeGeneratorService.Generate();
		UserRole studentRole = await GetRole(role: UserRoles.Student, cancellationToken: cancellationToken);

		UserActivityStatus activityStatus = await FindUserActivityStatus(
			activityStatus: UserActivityStatuses.Offline,
			cancellationToken: cancellationToken
		);

		if (!_context.Classes.AsNoTracking().Any(predicate: c => c.Id == request.ClassId))
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор класса.");

		try
		{
			User userPart = new User()
			{
				Surname = request.Surname,
				Name = request.Name,
				Patronymic = request.Patronymic,
				UserActivityStatusId = activityStatus.Id,
				RegistrationCode = registrationCode,
				UserRoleId = studentRole.Id
			};
			await _context.Users.AddAsync(entity: userPart, cancellationToken: cancellationToken);

			Student studentPart = new Student() { User = userPart, ClassId = request.ClassId };
			await _context.Students.AddAsync(entity: studentPart, cancellationToken: cancellationToken);

			await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		}
		catch (Exception _)
		{
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Произошла ошибка при сохранении.");
		}

		return Ok(value: new CreateUserResponse(RegistrationCode: registrationCode));
	}

	/// <summary>
	/// [Администратор] Добавление нового родителя обучающегося
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/parent/create
	///	{
	///		"Surname": "Фамилия",
	///		"Name": "Имя",
	///		"Patronymic": "Отчество",
	///		"ChildrenId": 1
	///	}
	///
	/// Параметры:
	///
	///	Surname - фамилия нового пользователя
	///	Name - имя нового пользователя
	///	Patronymic - отчество нового пользователя
	///	ChildrenId - идентификатор подопечного
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Регистрационный код пользователя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpPost(template: "parent/create")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CreateUserResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<CreateUserResponse>> CreateParent(
		[FromBody] CreateParentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string registrationCode = registrationCodeGeneratorService.Generate();
		UserRole parentRole = await GetRole(role: UserRoles.Parent, cancellationToken: cancellationToken);

		UserActivityStatus activityStatus = await FindUserActivityStatus(
			activityStatus: UserActivityStatuses.Offline,
			cancellationToken: cancellationToken
		);

		try
		{
			User userPart = new User()
			{
				Surname = request.Surname,
				Name = request.Name,
				Patronymic = request.Patronymic,
				UserActivityStatusId = activityStatus.Id,
				RegistrationCode = registrationCode,
				UserRoleId = parentRole.Id
			};
			await _context.Users.AddAsync(entity: userPart, cancellationToken: cancellationToken);

			Parent parentPart = new Parent() { User = userPart, ChildrenId = request.ChildrenId };
			await _context.Parents.AddAsync(entity: parentPart, cancellationToken: cancellationToken);

			await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		}
		catch (Exception _)
		{
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Произошла ошибка при сохранении.");
		}

		return Ok(value: new CreateUserResponse(RegistrationCode: registrationCode));
	}

	/// <summary>
	/// [Администратор] Добавление нового сотрудника администрации
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/parent/create
	///	{
	///		"Surname": "Фамилия",
	///		"Name": "Имя",
	///		"Patronymic": "Отчество"
	///	}
	///
	/// Параметры:
	///
	///	Surname - фамилия нового пользователя
	///	Name - имя нового пользователя
	///	Patronymic - отчество нового пользователя
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Регистрационный код пользователя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpPost(template: "create")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CreateUserResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<CreateUserResponse>> CreateAdministrator(
		[FromBody] CreateAdministratorRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string registrationCode = registrationCodeGeneratorService.Generate();
		UserRole administratorRole = await GetRole(role: UserRoles.Administrator, cancellationToken: cancellationToken);

		UserActivityStatus activityStatus = await FindUserActivityStatus(
			activityStatus: UserActivityStatuses.Offline,
			cancellationToken: cancellationToken
		);

		try
		{
			User userPart = new User()
			{
				Surname = request.Surname,
				Name = request.Name,
				Patronymic = request.Patronymic,
				UserActivityStatusId = activityStatus.Id,
				RegistrationCode = registrationCode,
				UserRoleId = administratorRole.Id
			};
			await _context.Users.AddAsync(entity: userPart, cancellationToken: cancellationToken);

			Administrator administratorPart = new Administrator() { User = userPart };
			await _context.Administrators.AddAsync(entity: administratorPart, cancellationToken: cancellationToken);

			await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		}
		catch (Exception _)
		{
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Произошла ошибка при сохранении.");
		}

		return Ok(value: new CreateUserResponse(RegistrationCode: registrationCode));
	}
	#endregion

	#region PUT
	/// <summary>
	/// [Администратор] Добавление новой преподаваемой дисциплины для преподавателя
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/administrator/teacher/subjects/add
	///	{
	///		"TeacherId": 1,
	///		"ClassId": 1,
	///		"SubjectId": 1
	///	}
	///
	/// Параметры:
	///
	///	TeacherId - идентификатор преподавателя
	///	ClassId - идентификатор учебного класса для обучения
	///	SubjectId - идентификатор преподаваемой дисциплины
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Регистрационный код добавленного преподавателя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpPost(template: "teacher/subjects/add")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult> AddTaughtSubjectForTeacher(
		[FromBody] AddTaughtSubjectForTeacherRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Teacher teacher = await _context.Teachers.FirstOrDefaultAsync(
			predicate: t => t.Id == request.TeacherId,
			cancellationToken: cancellationToken
		) ?? throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор преподавателя.");

		IQueryable<Class> @class = _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == request.ClassId);
		if (!@class.Any())
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор класса.");

		try
		{
			TeachersLesson teacherLesson = new TeachersLesson() { Teacher = teacher, LessonId = request.SubjectId, };
			await _context.TeachersLessons.AddAsync(entity: teacherLesson, cancellationToken: cancellationToken);
			await _context.SaveChangesAsync(cancellationToken: cancellationToken);

			teacherLesson.Classes = new List<Class>(@class);
			await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		}
		catch (Exception _)
		{
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Произошла ошибка при сохранении.");
		}

		return Ok();
	}
	#endregion
}