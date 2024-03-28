using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/student")]
[Authorize(Policy = nameof(UserRoles.Student))]
public sealed class StudentController(
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	#region Fields
	private readonly MyJournalContext _context = context;
	#endregion

	#region Records
	public sealed record GetStudentEducationPeriodsResponse(int Id, string Name, DateOnly StartDate, DateOnly EndDate);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// [Студент] Получение списка учебных периодов
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/student/periods/education/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список учебных периодов</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "periods/education/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetStudentEducationPeriodsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetStudentEducationPeriodsResponse>>> GetEducationPeriods(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<GetStudentEducationPeriodsResponse> educationPeriods = _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => new GetStudentEducationPeriodsResponse(
				epfc.EducationPeriod.Id,
				epfc.EducationPeriod.Period,
				epfc.EducationPeriod.StartDate,
				epfc.EducationPeriod.EndDate
			));

		return Ok(value: educationPeriods);
	}
	#endregion
	#endregion
}