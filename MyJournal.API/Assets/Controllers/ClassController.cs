using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[Route(template: "api/class")]
[ApiController]
public class ClassController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	public sealed record GetClassResponse(int Id, string Name);
	public sealed record GetEducationPeriodsByClassResponse(int Id, string Name);

	/// <summary>
	/// [Администратор] Получение списка классов
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/class/all/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список классов</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[HttpGet(template: "all/get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetClassResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetClassResponse>>> GetClasses(
		CancellationToken cancellationToken = default(CancellationToken)
	) => Ok(value: _context.Classes.AsNoTracking().Select(selector: c => new GetClassResponse(c.Id, c.Name)));

	/// <summary>
	/// Получение списка учебных периодов для класса
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/class/{classId:int}/periods/get
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список учебных периодов которого необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список классов</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[Authorize]
	[HttpGet(template: "{classId:int}/periods/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetEducationPeriodsByClassResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetEducationPeriodsByClassResponse>>> GetEducationPeriodsByClass(
		[FromRoute] int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<GetEducationPeriodsByClassResponse> educationPeriods = _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == classId)
			.SelectMany(selector: c => c.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Select(selector: ep => new GetEducationPeriodsByClassResponse(
				ep.Id,
				ep.Period
			));

		return Ok(value: educationPeriods);
	}
}
