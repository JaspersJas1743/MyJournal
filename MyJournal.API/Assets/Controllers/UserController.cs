using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.S3;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API.Assets.Controllers;

[Authorize]
[ApiController]
[Route(template: "api/user")]
public class UserController(
	MyJournalContext context,
	IHubContext<UserHub, IUserHub> userActivityHub,
	IFileStorageService fileStorageService
) : MyJournalBaseController(context: context)
{
	#region Records
	public record GetInforamtionResponse(string Surname, string Name, string? Patronymic, string? Phone, string? Email);
	public record GetUserInforamtionResponse(string Surname, string Name, string? Patronymic, string Activity, DateTime? OnlineAt);
	public record UploadProfilePhotoResponse(string Message);
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
	/// Загрузка фотографии профиля
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     GET api/user/profile/photo/download
	///
	/// </remarks>
	/// <response code="200">Возвращает фотографию профиля пользователя</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	/// <response code="404">Фотография пользователя не установлена</response>
	[HttpGet(template: "profile/photo/download")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult> DownloadProfilePhoto(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken) ??
					throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

		if (String.IsNullOrEmpty(user?.LinkToPhoto))
			throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Фотография пользователя не установлена");

		Stream file = await fileStorageService.GetFileAsync(key: user.LinkToPhoto, cancellationToken: cancellationToken);
		string fileName = Path.GetFileName(path: user.LinkToPhoto);
		string fileExtension = Path.GetExtension(path: fileName).Trim(trimChar: '.');

		return File(fileStream: file, contentType: $"image/{fileExtension}", fileDownloadName: fileName);
	}

	/// <summary>
	/// Получение основной информации
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     GET api/user/profile/info/me
	///
	/// </remarks>
	/// <response code="200">Возвращает основную информацию об авторизованном пользователе</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpGet(template: "profile/info/me")]
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
	/// Получение основной информации о пользователе по его идентификатору
	/// </summary>
	/// <param name="id">Идентификатор пользователя, информацию о котором необходимо получить</param>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     GET api/user/profile/info/{id:int}
	///
	/// </remarks>
	/// <response code="200">Возвращает основную информацию о пользователе</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpGet(template: "profile/info/{id:int}")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetUserInforamtionResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<GetUserInforamtionResponse>> GetUserInformation(
		[FromRoute] int id,
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

	#region PUT
	/// <summary>
	/// Изменяет статус активности на "В сети"
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     PUT api/user/profile/activity/online
	///
	/// </remarks>
	/// <response code="200">Статус "В сети" установлен успешно</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpPut(template: "profile/activity/online")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult> SetOnline(
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
	/// Изменяет статус активности на "Не в сети"
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     PUT api/user/profile/activity/offline
	///
	/// </remarks>
	/// <response code="200">Статус "Не в сети" установлен успешно</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpPut(template: "profile/activity/offline")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult> SetOffline(
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

	/// <summary>
	/// Установка фотографии профиля
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     PUT api/user/profile/photo/upload
	///
	/// </remarks>
	/// <response code="200">Возвращает сообщение об успешной установке фотографии профиля</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpPut(template: "profile/photo/upload")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(UploadProfilePhotoResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<UploadProfilePhotoResponse>> UploadProfilePhoto(
		IFormFile file,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken) ??
					throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

		string fileExtension = Path.GetExtension(path: file.FileName);
		string fileName = $"ProfilePhotos/id{user.Id}_profilephoto{fileExtension}";

		await fileStorageService.UploadFileAsync(key: fileName, fileStream: file.OpenReadStream(), cancellationToken: cancellationToken);
		context.Entry(entity: user).State = EntityState.Modified;
		user.LinkToPhoto = fileName;
		await context.SaveChangesAsync(cancellationToken: cancellationToken);
		return Ok(value: new UploadProfilePhotoResponse(Message: "Фотография сохранена успешно!"));
	}
	#endregion
	#endregion
}
