using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/subjects")]
public sealed class LessonController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Records
	public sealed record Teacher(int Id, string Surname, string Name, string? Patronymic);
	public sealed record GetStudyingSubjectsResponse(int Id, string Name, Teacher Teacher);
	public sealed record Class(int Id, string Name);
	public sealed record GetTaughtSubjectsResponse(int Id, string Name, Class Class);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// [Студент] Получение списка дисциплин, преподаваемых обучающемуся в текущий учебный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/studying/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, изучаемых в текущем году</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "studying/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudyingSubjectsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudyingSubjectsResponse>>> GetStudyingSubjects(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly nowDate = DateOnly.FromDateTime(dateTime: DateTime.Now);
		IQueryable<GetStudyingSubjectsResponse> learnedSubjects = _context.Students
			.Where(s => s.UserId == userId)
			.SelectMany(s => s.Class.EducationPeriodForClasses)
			.Where(epfc =>
				EF.Functions.DateDiffDay(epfc.EducationPeriod.StartDate, nowDate) >= 0 &&
				EF.Functions.DateDiffDay(nowDate, epfc.EducationPeriod.EndDate) <= 0
			).SelectMany(epfc => epfc.Lessons)
			.SelectMany(l => l.TeachersLessons.Where(tl => tl.LessonId == l.Id).Select(tl => new GetStudyingSubjectsResponse(
				l.Id, l.Name, new Teacher(tl.Teacher.Id, tl.Teacher.User.Surname, tl.Teacher.User.Name, tl.Teacher.User.Patronymic)
			)));

		return Ok(value: learnedSubjects);
	}

	/// <summary>
	/// [Преподаватель] Получение списка преподаваемых дисциплин по классам в текущий учебный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/taught/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список преподаваемых дисциплин по классам в текущем году</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "taught/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetTaughtSubjectsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetTaughtSubjectsResponse>>> GetTaughtSubjects(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly nowDate = DateOnly.FromDateTime(dateTime: DateTime.Now);
		IQueryable<GetTaughtSubjectsResponse> taughtSubjects = _context.Teachers
			.Where(t => t.UserId == userId)
			.SelectMany(t => t.TeachersLessons)
			.Where(tl => tl.Classes.Any(c => c.EducationPeriodForClasses.Any(epfc =>
				EF.Functions.DateDiffDay(epfc.EducationPeriod.StartDate, nowDate) >= 0 &&
				EF.Functions.DateDiffDay(nowDate, epfc.EducationPeriod.EndDate) <= 0
			))).SelectMany(tl => tl.Classes.Select(c => new GetTaughtSubjectsResponse(
				tl.LessonId, tl.Lesson.Name, new Class(c.Id, c.Name)
			)));

		return Ok(value: taughtSubjects);
	}

	/// <summary>
	/// [Родитель] Получение списка дисциплин, преподаваемых подопечному в текущий учебный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/children/studying/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, преподаваемых подопечному в текущем году</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[HttpGet(template: "children/studying/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudyingSubjectsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudyingSubjectsResponse>>> GetSubjectsStudiedByWard(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly nowDate = DateOnly.FromDateTime(dateTime: DateTime.Now);
		IQueryable<GetStudyingSubjectsResponse> learnedSubjects = _context.Parents
			.Where(p => p.UserId == userId)
			.SelectMany(p => p.Children.Class.EducationPeriodForClasses)
			.Where(epfc =>
				EF.Functions.DateDiffDay(epfc.EducationPeriod.StartDate, nowDate) >= 0 &&
				EF.Functions.DateDiffDay(nowDate, epfc.EducationPeriod.EndDate) <= 0
			).SelectMany(epfc => epfc.Lessons)
			.SelectMany(l => l.TeachersLessons.Where(tl => tl.LessonId == l.Id).Select(tl => new GetStudyingSubjectsResponse(
				l.Id, l.Name, new Teacher(tl.Teacher.Id, tl.Teacher.User.Surname, tl.Teacher.User.Name, tl.Teacher.User.Patronymic)
			)));

		return Ok(value: learnedSubjects);
	}

	/// <summary>
	/// [Администратор] Получение списка дисциплин, преподаваемых указанному классу в текущий учебный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/{classId}/get
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список дисциплин которого необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, преподаваемых указанному классу в текущем году</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "{classId:int}/get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudyingSubjectsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudyingSubjectsResponse>>> GetSubjectsStudiedInClass(
		[FromRoute] int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<GetStudyingSubjectsResponse> studyingSubjects = _context.Classes
			.Where(c => c.Id == classId)
			.SelectMany(c => c.TeachersLessons)
			.Select(tl => new GetStudyingSubjectsResponse(
				tl.LessonId, tl.Lesson.Name, new Teacher(tl.Teacher.Id, tl.Teacher.User.Surname, tl.Teacher.User.Name, tl.Teacher.User.Patronymic)
			));

		return Ok(value: studyingSubjects);
	}
	#endregion
	#endregion
}