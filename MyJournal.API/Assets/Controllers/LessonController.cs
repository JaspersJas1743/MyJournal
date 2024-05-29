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
	public sealed record GetSubjectsResponse(int Id, string Name);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// [Студент] Получение списка дисциплин, преподаваемых обучающемуся
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/studying/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, изучаемых в текущем учебном периоде</response>
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
		IQueryable<GetStudyingSubjectsResponse> learnedSubjects = _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.Select(selector: s => s.Class)
			.SelectMany(selector: c => c.TeachersLessons)
			.OrderBy(keySelector: tl => tl.Lesson.Name)
			.Select(selector: tl => new GetStudyingSubjectsResponse(
				tl.LessonId,
				tl.Lesson.Name,
				new Teacher(
					tl.Teacher.Id,
					tl.Teacher.User.Surname,
					tl.Teacher.User.Name,
					tl.Teacher.User.Patronymic
				)
			));

		return Ok(value: learnedSubjects);
	}

	/// <summary>
	/// [Преподаватель] Получение списка преподаваемых дисциплин по классам
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/taught/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список преподаваемых дисциплин по классам в текущем учебном периоде</response>
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
		IEnumerable<GetTaughtSubjectsResponse> taughtSubjects = _context.Teachers.AsNoTracking()
			.Where(predicate: t => t.UserId == userId)
			.SelectMany(selector: t => t.TeachersLessons)
			.SelectMany(selector: tl => tl.Classes
				.Select(c => new GetTaughtSubjectsResponse(
					 tl.LessonId,
					 tl.Lesson.Name,
					 new Class(
						 c.Id,
						 c.Name
					 )
				))
			)
			.AsEnumerable()
			.OrderBy(keySelector: r => r.Class.Id)
			.ThenBy(keySelector: r => r.Name);

		return Ok(value: taughtSubjects);
	}

	/// <summary>
	/// [Родитель] Получение списка дисциплин, преподаваемых подопечному
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/children/studying/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, преподаваемых подопечному в текущем учебном периоде</response>
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
		IQueryable<GetStudyingSubjectsResponse> learnedSubjects = _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.Select(selector: p => p.Children.Class)
			.SelectMany(selector: c => c.TeachersLessons)
			.OrderBy(keySelector: tl => tl.Lesson.Name)
			.Select(selector: tl => new GetStudyingSubjectsResponse(
				tl.LessonId,
				tl.Lesson.Name,
				new Teacher(
					tl.Teacher.Id,
					tl.Teacher.User.Surname,
					tl.Teacher.User.Name,
					tl.Teacher.User.Patronymic
				)
			));

		return Ok(value: learnedSubjects);
	}

	/// <summary>
	/// [Администратор] Получение списка дисциплин, преподаваемых указанному классу
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/class/{classId}/taught/get
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список дисциплин которого необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, преподаваемых указанному классу в текущем учебном периоде</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "class/{classId:int}/taught/get")]
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
		IQueryable<GetStudyingSubjectsResponse> studyingSubjects = _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == classId)
			.SelectMany(selector: c => c.TeachersLessons)
			.OrderBy(keySelector: tl => tl.Lesson.Name)
			.Select(selector: tl => new GetStudyingSubjectsResponse(
				tl.LessonId,
				tl.Lesson.Name,
				new Teacher(
					tl.Teacher.Id,
					tl.Teacher.User.Surname,
					tl.Teacher.User.Name,
					tl.Teacher.User.Patronymic
				)
			));

		return Ok(value: studyingSubjects);
	}

	/// <summary>
	/// [Администратор] Получение списка дисциплин, преподаваемых указанному классу
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/subjects/class/{classId}/get
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список дисциплин которого необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список дисциплин, изучаемых в указанном классе</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "class/{classId:int}/all/get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudyingSubjectsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudyingSubjectsResponse>>> GetSubjectsForClass(
		[FromRoute] int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<GetSubjectsResponse> subjects = _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == classId)
			.SelectMany(selector: c => c.EducationPeriodForClasses)
			.SelectMany(selector: epfc => epfc.Lessons)
			.Distinct().OrderBy(keySelector: r => r.Name)
			.Select(selector: l => new GetSubjectsResponse(l.Id, l.Name));

		return Ok(value: subjects);
	}
	#endregion
	#endregion
}