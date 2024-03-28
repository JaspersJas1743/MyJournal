using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/teacher")]
[Authorize(Policy = nameof(UserRoles.Teacher))]
public sealed class TeacherController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	public sealed record GetTeacherEducationPeriodsResponse(int Id, string Name, DateOnly StartDate, DateOnly EndDate);

	/// <summary>
	/// [Преподаватель] Получение списка учебных периодов, в которые ведет преподаватель
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/teacher/periods/education/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список классов</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "periods/education/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetTeacherEducationPeriodsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetTeacherEducationPeriodsResponse>>> GetEducationPeriods(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<GetTeacherEducationPeriodsResponse> educationPeriods = _context.Teachers.AsNoTracking()
			.Where(predicate: t => t.UserId == userId)
			.SelectMany(selector: t => t.TeachersLessons)
			.SelectMany(selector: tl => tl.Classes)
			.SelectMany(selector: c => c.EducationPeriodForClasses)
			.Select(selector: epfc => new GetTeacherEducationPeriodsResponse(
				epfc.EducationPeriod.Id,
				epfc.EducationPeriod.Period,
				epfc.EducationPeriod.StartDate,
				epfc.EducationPeriod.EndDate
			));

		return Ok(value: educationPeriods);
	}
}