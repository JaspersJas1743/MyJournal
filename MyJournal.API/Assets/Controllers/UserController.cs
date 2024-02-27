using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API.Assets.Controllers;

[Authorize]
[ApiController]
[Route(template: "api/[controller]/[action]")]
public class UserController(
	MyJournalContext context,
	IHubContext<UserHub, IUserHub> userActivityHub
) : MyJournalBaseController(context: context)
{
	#region Records
	public record GetInforamtionResponse(string Surname, string Name, string? Patronymic, string? Phone, string? Email);
	public record GetUserInforamtionResponse(string Surname, string Name, string? Patronymic, string Activity, DateTime? OnlineAt);
	#endregion

	#region Methods
	#region AuxiliaryMethods
	private async Task<UserActivityStatus> GetActivityStatus(
		UserActivityStatuses activityStatus,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await context.UserActivityStatuses.FirstAsync(
			predicate: status => status.ActivityStatus.Equals(activityStatus),
			cancellationToken: cancellationToken
		);
	}

	private async Task<int> SetActivityStatus(
		UserActivityStatuses activityStatus,
		DateTime? onlineAt,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User? user = await GetAuthorizedUser(cancellationToken: cancellationToken) ??
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

		user.UserActivityStatus = await GetActivityStatus(
			activityStatus: activityStatus,
			cancellationToken: cancellationToken
		);
		user.OnlineAt = onlineAt;
		await context.SaveChangesAsync(cancellationToken: cancellationToken);
		return user.Id;
	}

	private async Task<User?> FindUserByIdAsync(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await context.Users.Include(navigationPropertyPath: user => user.UserActivityStatus)
								  .SingleOrDefaultAsync(
									  predicate: user => user.Id.Equals(id),
									  cancellationToken: cancellationToken
								  );
	}
	#endregion

	#region GET
	/// <summary>
	/// Получение основных данных пользователя
	/// </summary>
	/// <remarks>
	/// Sample request:
	///
	///     GET /GetInformation
	///
	/// </remarks>
	/// <response code="200">Возвращает основную информацию о пользователе</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpGet]
	[Authorize]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetInforamtionResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<GetInforamtionResponse>> GetInformation(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User? user = await GetAuthorizedUser(cancellationToken: cancellationToken) ??
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

		return Ok(value: new GetInforamtionResponse(
			Surname: user.Surname,
			Name: user.Name,
			Patronymic: user.Patronymic,
			Phone: user.Phone,
			Email: user.Email
		));
	}

	/// <summary>
	/// Получение основных данных пользователя
	/// </summary>
	/// <remarks>
	/// Sample request:
	///
	///     GET /GetUserInformation/{id}
	///
	/// </remarks>
	/// <response code="200">Возвращает основную информацию о пользователе</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[Authorize]
	[HttpGet(template: "{id:int}")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetUserInforamtionResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<GetUserInforamtionResponse>> GetUserInformation(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User? user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
					 throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный идентификатор пользователя.");

		return Ok(value: new GetUserInforamtionResponse(
					  Surname: user.Surname,
					  Name: user.Name,
					  Patronymic: user.Patronymic,
					  Activity: user.UserActivityStatus.ActivityStatus.ToString(),
					  OnlineAt: user.OnlineAt
				  ));
	}
	#endregion

	#region POST
	/// <summary>
	/// Устанавливает статус активности пользователя как "В сети"
	/// </summary>
	/// <remarks>
	/// Sample request:
	///
	///     POST /SetOnline
	///
	/// </remarks>
	/// <response code="200">Статус "В сети" установлен успешно</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpPost]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<GetInforamtionResponse>> SetOnline(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = await SetActivityStatus(
			activityStatus: UserActivityStatuses.Online,
			onlineAt: null,
			cancellationToken: cancellationToken
		);
		await userActivityHub.Clients.All.SetOnline(userId: userId);
		return Ok();
	}

	/// <summary>
	/// Устанавливает статус активности пользователя как "Не в сети"
	/// </summary>
	/// <remarks>
	/// Sample request:
	///
	///     POST /SetOffline
	///
	/// </remarks>
	/// <response code="200">Статус "Не в сети" установлен успешно</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpPost]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<GetInforamtionResponse>> SetOffline(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = await SetActivityStatus(
			activityStatus: UserActivityStatuses.Offline,
			onlineAt: DateTime.Now,
			cancellationToken: cancellationToken
		);
		await userActivityHub.Clients.All.SetOffline(userId: userId);
		return Ok();
	}

	#endregion
	#endregion
}
