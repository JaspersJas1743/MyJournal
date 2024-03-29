using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/parent")]
[Authorize(Policy = nameof(UserRoles.Parent))]
public sealed class ParentController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	public sealed record GetParentEducationPeriodsResponse(int Id, string Name, DateOnly StartDate, DateOnly EndDate);

	/// <summary>
	/// [Родитель] Получение списка учебных периодов подопечного
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/parent/periods/education/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список учебных периодов подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[HttpGet(template: "periods/education/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetParentEducationPeriodsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetParentEducationPeriodsResponse>>> GetEducationPeriods(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<GetParentEducationPeriodsResponse> educationPeriods = _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.SelectMany(selector: t => t.Children.Class.EducationPeriodForClasses)
			.Select(selector: epfc => new GetParentEducationPeriodsResponse(
				epfc.EducationPeriod.Id,
				epfc.EducationPeriod.Period,
				epfc.EducationPeriod.StartDate,
				epfc.EducationPeriod.EndDate
			));

		return Ok(value: educationPeriods);
	}
}