using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/timetable")]
public sealed class TimetableController(
	MyJournalContext context,
	ILogger<TimetableController> logger,
	IHubContext<TeacherHub, ITeacherHub> teacherHubContext,
	IHubContext<StudentHub, IStudentHub> studentHubContext,
	IHubContext<ParentHub, IParentHub> parentHubContext,
	IHubContext<AdministratorHub, IAdministratorHub> administratorHubContext
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Records
	[Validator<GetTimetableByDateRequestValidator>]
	public sealed record GetTimetableByDateRequest(DateOnly Day);

	[Validator<GetTimetableBySubjectRequestValidator>]
	public sealed record GetTimetableBySubjectRequest(int SubjectId);

	[Validator<GetTimetableBySubjectAndClassRequestValidator>]
	public sealed record GetTimetableBySubjectAndClassRequest(int SubjectId, int ClassId);

	public sealed record GetTimetableResponseSubject(int Id, int Number, string ClassName, string Name, DateOnly Date, TimeSpan Start, TimeSpan End);
	public sealed record Estimation(string Grade);
	public sealed record Break(double CountMinutes);
	public sealed record GetTimetableWithAssessmentsResponse(GetTimetableResponseSubject Subject, IEnumerable<Estimation> Estimations, Break? Break)
	{
		public Break? Break { get; set; } = Break;
	}
	public sealed record GetTimetableWithoutAssessmentsResponse(GetTimetableResponseSubject Subject, Break? Break)
	{
		public Break? Break { get; set; } = Break;
	}

	[Validator<GetTimetableByClassRequestValidator>]
	public sealed record GetTimetableByClassRequest(int ClassId);
	public sealed record DayOfWeekOnTimetable(int Id, string Name);
	public sealed record ShortSubject(int Id, int Number, string Name, TimeSpan Start, TimeSpan End);
	public sealed record GetTimetableByClassResponseSubject(ShortSubject Subject, Break? Break)
	{
		public ShortSubject Subject { get; set; } = Subject;
		public Break? Break { get; set; } = Break;
	}

	public sealed record GetTimetableByClassResponse(DayOfWeekOnTimetable DayOfWeek, int TotalHours, IEnumerable<GetTimetableByClassResponseSubject> Subjects)
	{
		public IEnumerable<GetTimetableByClassResponseSubject> Subjects { get; set; } = Subjects;
	}

	[Validator<CreateTimetableRequestValidator>]
	public sealed record CreateTimetableRequest(int ClassId, IEnumerable<Shedule> Timetable);
	public sealed record SubjectOnTimetable(int Id, int Number, TimeSpan Start, TimeSpan End);
	public sealed record Shedule(int DayOfWeekId, IEnumerable<SubjectOnTimetable> Subjects);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// [Ученик] Получение расписания ученика с группировкой по дате
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/date/student/get?Day=2024.04.01
	///
	/// Параметры:
	///
	///	Day - дата, за которую необходимо получить расписание
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание ученика</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "date/student/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableWithAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableByDateForStudent(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		logger.LogCritical($"request={request.Day}");
		int userId = GetAuthorizedUserId();
		GetTimetableWithAssessmentsResponse[] timings = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.Where(predicate: s => !s.Class.EducationPeriodForClasses.Any(
				epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < request.Day && h.EndDate > request.Day)
			))
			.SelectMany(selector: s => s.Class.LessonTimings)
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableWithAssessmentsResponse(
				new GetTimetableResponseSubject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				t.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					t.StartTime <= a.Datetime.TimeOfDay &&
					t.EndTime >= a.Datetime.TimeOfDay &&
					a.Student.UserId == userId
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = timings.Select(selector: (timing, index) =>
		{
			GetTimetableWithAssessmentsResponse? nextTiming = timings.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: timetable);
	}

	/// <summary>
	/// [Ученик] Получение расписания ученика с группировкой по дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/subject/student/get?SubjectId=0
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, расписание по которой необхоимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание ученика</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "subject/student/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableWithAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableBySubjectForStudent(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset)))
			.SelectMany(selector: d => _context.Students.AsNoTracking()
				.Where(predicate: s => s.UserId == userId)
				.Where(predicate: s => !s.Class.EducationPeriodForClasses.Any(
					epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < d && h.EndDate > d)
				))
				.SelectMany(selector: s => s.Class.LessonTimings)
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableWithAssessmentsResponse(
					new GetTimetableResponseSubject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, d, t.StartTime, t.EndTime),
					t.Lesson.Assessments.Where(a =>
						DateOnly.FromDateTime(a.Datetime) == d &&
						t.StartTime <= a.Datetime.TimeOfDay &&
						t.EndTime >= a.Datetime.TimeOfDay &&
						a.Student.UserId == userId
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}

	/// <summary>
	/// [Родитель] Получение расписания подопечного с группировкой по дате
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/date/parent/get?Day=2024.04.01
	///
	/// Параметры:
	///
	///	Day - дата, на которую надо получить расписание
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[HttpGet(template: "date/parent/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableWithAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableByDateForParent(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableWithAssessmentsResponse[] timings = await _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.Where(predicate: p => !p.Children.Class.EducationPeriodForClasses.Any(
				epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < request.Day && h.EndDate > request.Day)
			))
			.SelectMany(selector: p => p.Children.Class.LessonTimings)
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableWithAssessmentsResponse(
				new GetTimetableResponseSubject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				t.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					t.StartTime <= a.Datetime.TimeOfDay &&
					t.EndTime >= a.Datetime.TimeOfDay &&
					a.Student.Parents.Any(p => p.UserId == userId)
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = timings.Select(selector: (t, i) =>
		{
			GetTimetableWithAssessmentsResponse? nextTiming = timings.ElementAtOrDefault(index: i + 1);
			if (nextTiming is not null)
				t.Break = new Break(CountMinutes: (nextTiming.Subject.Start - t.Subject.End).TotalMinutes);
			return t;
		});

		return Ok(value: timetable);
	}

	/// <summary>
	/// [Родитель] Получение расписания подопечного с группировкой по дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/subject/parent/get?SubjectId=0
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, расписание по которой надо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[HttpGet(template: "subject/parent/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableWithAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableBySubjectForParent(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset)))
			.SelectMany(selector: d => _context.Parents.AsNoTracking()
				.Where(predicate: p => p.UserId == userId)
				.Where(predicate: p => !p.Children.Class.EducationPeriodForClasses.Any(
					epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < d && h.EndDate > d)
				))
				.SelectMany(selector: p => p.Children.Class.LessonTimings)
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableWithAssessmentsResponse(
					new GetTimetableResponseSubject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, d, t.StartTime, t.EndTime),
					t.Lesson.Assessments.Where(a =>
						DateOnly.FromDateTime(a.Datetime) == d &&
						t.StartTime <= a.Datetime.TimeOfDay &&
						t.EndTime >= a.Datetime.TimeOfDay &&
						a.Student.Parents.Any(p => p.UserId == userId)
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}

	/// <summary>
	/// [Преподаватель] Получение расписания преподавателя с группировкой по дате
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/date/teacher/get?Day=2024.04.01
	///
	/// Параметры:
	///
	///	Day - дата, на которую надо получить расписание
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание преподавателя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "date/teacher/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableWithoutAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableWithoutAssessmentsResponse>> GetTimetableByDateForTeacher(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableWithoutAssessmentsResponse[] timings = await _context.Teachers.AsNoTracking()
			.Where(predicate: t => t.UserId == userId)
			.SelectMany(selector: t => t.TeachersLessons)
			.SelectMany(selector: l => l.Lesson.LessonTimings.Intersect(l.Classes.SelectMany(c => c.LessonTimings)))
			.Where(predicate: t => !t.Class.EducationPeriodForClasses.Any(
				epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < request.Day && h.EndDate > request.Day)
			))
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableWithoutAssessmentsResponse(
				new GetTimetableResponseSubject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableWithoutAssessmentsResponse> timetable = timings.Select(selector: (timing, index) =>
		{
			GetTimetableWithoutAssessmentsResponse? nextTiming = timings.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: timetable);
	}

	/// <summary>
	/// [Преподаватель] Получение расписания преподавателя с группировкой по дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/subject/teacher/get?SubjectId=0
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, расписание по которой надо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание преподавателя</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "subject/teacher/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableWithoutAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableWithoutAssessmentsResponse>> GetTimetableBySubjectForTeacher(
		[FromQuery] GetTimetableBySubjectAndClassRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableWithoutAssessmentsResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset)))
			.SelectMany(selector: d => _context.Teachers.AsNoTracking()
				.Where(predicate: t => t.UserId == userId)
				.SelectMany(selector: t => t.TeachersLessons)
				.SelectMany(selector: l => l.Lesson.LessonTimings.Intersect(l.Classes.SelectMany(c => c.LessonTimings)))
				.Where(predicate: t => !t.Class.EducationPeriodForClasses.Any(
					epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < d && h.EndDate > d)
				))
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek &&
					t.LessonId == request.SubjectId && t.ClassId == request.ClassId
				).OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableWithoutAssessmentsResponse(
					new GetTimetableResponseSubject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, d, t.StartTime, t.EndTime),
					null
				))
			);
		return Ok(value: timetable);
	}

	/// <summary>
	/// [Администратор] Получение расписания класса
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/timetable/get?ClassId=0
	///
	/// Параметры:
	///
	///	ClassId - идентификатор класса, для которог онадо получить расписание
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание класса</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetTimetableByClassResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetTimetableByClassResponse>> GetTimetableForClass(
		[FromQuery] GetTimetableByClassRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetTimetableByClassResponse[] timings = await _context.DaysOfWeeks.AsNoTracking()
			.Where(predicate: d => d.TypeOfDayNavigation.DayType == TypesOfDay.WorkingDay)
			.Select(selector: d => new GetTimetableByClassResponse(
				new DayOfWeekOnTimetable(d.Id, d.DayOfWeek),
				d.LessonTimings.Count(t => t.ClassId == request.ClassId),
				d.LessonTimings.Where(t => t.ClassId == request.ClassId)
					.Select(t => new GetTimetableByClassResponseSubject(
						new ShortSubject(t.LessonId, t.Number, t.Lesson.Name, t.StartTime, t.EndTime),
						null
					))
			)).ToArrayAsync(cancellationToken: cancellationToken);

		foreach (GetTimetableByClassResponse timing in timings)
		{
			for (int i = 0; i < timing.Subjects.Count(); ++i)
			{
				GetTimetableByClassResponseSubject? currentTiming = timing.Subjects.ElementAtOrDefault(index: i);
				GetTimetableByClassResponseSubject? nextTiming = timing.Subjects.ElementAtOrDefault(index: i + 1);
				if (nextTiming is not null)
					currentTiming!.Break = new Break(CountMinutes: (nextTiming.Subject.Start - currentTiming.Subject.End).TotalMinutes);
			}
			timing.Subjects = timing.Subjects.OrderBy(t => t.Subject.Number);
		}

		return Ok(value: timings);
	}
	#endregion

	#region PUT
	/// <summary>
	/// [Администратор] Создание и/или изменение расписания класса
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/timetable/create
	///	{
	///		"ClassId": 1,
	///		"Timetable": [
	///			{
	///				"DayOfWeekId": 0,
	///				"Subjects": [
	///					{
	///						"Id": 0,
	///						"Number": 0,
	///						"Start": "00:00:00",
	///						"End": "00:00:00"
	///					}
	///				]
	///			}
	///		]
	///	}
	///
	/// Параметры:
	///
	///	ClassId - идентификатор класса, для которого создается расписание
	///	Timetable.DayOfWeekId - идентификатор дня недели, на который будут записаны дисциплины из списка Subjects
	///	Timetable.Subjects.Id - идентификатор проводимой дисциплины
	///	Timetable.Subjects.Number - номер учебного занятия по порядку
	///	Timetable.Subjects.Start - время начала заняти
	///	Timetable.Subjects.End - время окончания занятия
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Расписание сохранено успешно</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpPut(template: "create")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult> CreateTimetable(
		[FromBody] CreateTimetableRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<LessonTiming> addedTimings = request.Timetable.SelectMany(
			selector: t => t.Subjects.Select(selector: s => new LessonTiming()
			{
				LessonId = s.Id,
				ClassId = request.ClassId,
				DayOfWeekId = t.DayOfWeekId,
				StartTime = s.Start,
				EndTime = s.End,
				Number = s.Number
			})
		);

		_context.RemoveRange(entities: _context.LessonTimings.AsNoTracking().Where(predicate: t => t.ClassId == request.ClassId));
		await _context.LessonTimings.AddRangeAsync(entities: addedTimings, cancellationToken: cancellationToken);

		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IQueryable<string> studentIds = _context.Students.AsNoTracking().Where(predicate: s => s.ClassId == request.ClassId)
			.Select(selector: s => s.UserId.ToString());

		IQueryable<string> teacherIds = _context.Teachers.AsNoTracking().Where(predicate: t => t.TeachersLessons.Any(
			l => l.Classes.Any(c => c.Id == request.ClassId)
		)).Select(selector: t => t.UserId.ToString());

		IQueryable<string> parentIds = _context.Parents.AsNoTracking().Where(predicate: p => p.Children.ClassId == request.ClassId)
			.Select(selector: p => p.UserId.ToString());

		IQueryable<string> administratorIds = _context.Administrators.AsNoTracking().Select(selector: a => a.UserId.ToString());

		await studentHubContext.Clients.Users(userIds: studentIds).ChangedTimetable();
		await parentHubContext.Clients.Users(userIds: parentIds).ChangedTimetable();
		await teacherHubContext.Clients.Users(userIds: teacherIds).ChangedTimetable(classId: request.ClassId);
		await administratorHubContext.Clients.Users(userIds: administratorIds).ChangedTimetable(classId: request.ClassId);

		return Ok();
	}
	#endregion
	#endregion
}