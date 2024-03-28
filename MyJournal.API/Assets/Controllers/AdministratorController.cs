using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/administrator")]
[Authorize(Policy = nameof(UserRoles.Administrator))]
public sealed class AdministratorController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	public sealed record GetTeacherEducationPeriodsResponse(int Id, string Name, DateOnly StartDate, DateOnly EndDate);

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
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[HttpGet(template: "periods/education/class/{classId:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetTeacherEducationPeriodsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetTeacherEducationPeriodsResponse>>> GetEducationPeriods(
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
}