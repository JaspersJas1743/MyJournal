using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/class")]
public class ClassController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	public sealed record GetClassResponse(int Id, string Name);
	public sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);

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
	/// [Преподаватель/Администратор] Получение списка учеников класса
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/class/{classId:int}/students/get
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список учеников которого необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список учеников класса</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher или Administrator</response>
	[HttpGet(template: "{classId:int}/students/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudentsFromClassResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudentsFromClassResponse>>> GetStudentsFromClass(
		[FromRoute] int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return Ok(value: _context.Classes.Where(predicate: c => c.Id == classId).SelectMany(c => c.Students)
			.Select(selector: s => new GetStudentsFromClassResponse(s.Id, s.User.Surname, s.User.Name, s.User.Patronymic))
			.OrderBy(keySelector: r => r.Surname)
			.ThenBy(keySelector: r => r.Name)
			.ThenBy(keySelector: r => r.Patronymic)
		);
	}
}
