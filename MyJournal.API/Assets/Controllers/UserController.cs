using System.Diagnostics;
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
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;

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
	public record GetInforamtionResponse(string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	public record GetUserInforamtionResponse(string Surname, string Name, string? Patronymic, string? Photo, string Activity, DateTime? OnlineAt);
	[Validator<UploadProfilePhotoRequestValidator>]
	public record UploadProfilePhotoRequest(IFormFile File);
	public record UploadProfilePhotoResponse(string Link);
	public record DownloadProfilePhotoResponse(string Link);
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

	private async Task<(int id, DateTime? onlineAt)> SetActivityStatus(
		UserActivityStatuses activityStatus,
		DateTime? onlineAt,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		user.UserActivityStatus = await GetActivityStatus(
			activityStatus: activityStatus,
			cancellationToken: cancellationToken
		);
		user.OnlineAt = onlineAt;
		await context.SaveChangesAsync(cancellationToken: cancellationToken);
		return (id: user.Id, onlineAt: onlineAt);
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
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		if (String.IsNullOrEmpty(user?.LinkToPhoto))
			throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Фотография пользователя не установлена");

		FileInfo fileInfo = new FileInfo(fileName: user.LinkToPhoto);
		Stream file = await fileStorageService.GetFileAsync(key: $"{fileInfo.Directory?.Name}/{fileInfo.Name}", cancellationToken: cancellationToken);
		string fileExtension = fileInfo.Extension.Trim(trimChar: '.');

		return File(fileStream: file, contentType: $"image/{fileExtension}", fileDownloadName: $"{user.Surname} {user.Name} {user.Patronymic}.{fileExtension}");
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
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		return Ok(value: new GetInforamtionResponse(
			Surname: user.Surname,
			Name: user.Name,
			Patronymic: user.Patronymic,
			Phone: user.Phone,
			Email: user.Email,
			Photo: user.LinkToPhoto
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
			OnlineAt: user.OnlineAt,
			Photo: user.LinkToPhoto
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
		(int userId, DateTime? onlineAt) = await SetActivityStatus(
			activityStatus: UserActivityStatuses.Online,
			onlineAt: null,
			cancellationToken: cancellationToken
		);
		await userActivityHub.Clients.All.SetOnline(userId: userId, onlineAt: onlineAt);
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
		(int userId, DateTime? onlineAt) = await SetActivityStatus(
			activityStatus: UserActivityStatuses.Offline,
			onlineAt: DateTime.Now,
			cancellationToken: cancellationToken
		);
		await userActivityHub.Clients.All.SetOffline(userId: userId, onlineAt: onlineAt);
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
	/// <response code="200">Возвращает ссылку на фотографию профиля пользователя</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpPut(template: "profile/photo/upload")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(UploadProfilePhotoResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult<UploadProfilePhotoResponse>> UploadProfilePhoto(
		[FromForm] UploadProfilePhotoRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		string fileExtension = Path.GetExtension(path: request.File.FileName);
		string fileKey = $"ProfilePhotos/id{user.Id}_profilephoto{fileExtension}";

		string link = await fileStorageService.UploadFileAsync(key: fileKey, fileStream: request.File.OpenReadStream(), cancellationToken: cancellationToken);
		context.Entry(entity: user).State = EntityState.Modified;
		user.LinkToPhoto = link;
		await context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userActivityHub.Clients.All.UpdatedProfilePhoto(userId: user.Id);

		return Ok(value: new UploadProfilePhotoResponse(Link: link));
	}
	#endregion

	#region DELETE
	/// <summary>
	/// Удаление фотографии профиля
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     DELETE api/user/profile/photo/delete
	///
	/// </remarks>
	/// <response code="200">Фотография профиля пользователя удалена успешна</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован</response>
	[HttpDelete(template: "profile/photo/delete")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
	public async Task<ActionResult> DeleteProfilePhoto(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		string? extension = Path.GetExtension(path: user.LinkToPhoto);
		await fileStorageService.DeleteFileAsync(key: $"ProfilePhotos/id{user.Id}_profilephoto{extension}", cancellationToken: cancellationToken);
		context.Entry(entity: user).State = EntityState.Modified;
		user.LinkToPhoto = null;
		await context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userActivityHub.Clients.All.DeletedProfilePhoto(userId: user.Id);

		return Ok();
	}
	#endregion
	#endregion
}
