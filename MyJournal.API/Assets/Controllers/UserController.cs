using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.GoogleAuthenticator;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.S3;
using MyJournal.API.Assets.Security.Hash;
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
	IFileStorageService fileStorageService,
	IHashService hashService,
	IGoogleAuthenticatorService googleAuthenticatorService
) : MyJournalBaseController(context: context)
{
	#region Records
	public record GetInforamtionResponse(string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	public record GetUserInforamtionResponse(string Surname, string Name, string? Patronymic, string? Photo, string Activity, DateTime? OnlineAt);
	[Validator<UploadProfilePhotoRequestValidator>]
	public record UploadProfilePhotoRequest(IFormFile File);
	public record UploadProfilePhotoResponse(string Link);

	public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
	public record ChangePasswordResponse(string Message);
	#endregion

	#region Methods
	#region AuxiliaryMethods
	private async Task<UserActivityStatus> GetUserActivityStatus(
		UserActivityStatuses activityStatus,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await context.UserActivityStatuses.FirstAsync(
			predicate: status => status.ActivityStatus.Equals(activityStatus),
			cancellationToken: cancellationToken
		);
	}

	private async Task<SessionActivityStatus> GetSessionActivityStatus(
		SessionActivityStatuses activityStatus,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await context.SessionActivityStatuses.FirstAsync(
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

		user.UserActivityStatus = await GetUserActivityStatus(
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

	/// <summary>
	/// Возвращает значение, является ли код, введенный пользователем, верным
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     GET api/user/profile/security/code/verify?UserCode=`your_code`
	///
	/// </remarks>
	/// <response code="200">Возвращает статус кода от пользователя: true - верный, false - неверный</response>
	/// <response code="404">Некорректный идентификатор пользователя</response>
	[HttpGet(template: "profile/security/code/verify")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(AccountController.VerifyGoogleAuthenticatorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	public async Task<ActionResult<AccountController.VerifyGoogleAuthenticatorResponse>> VerifyGoogleAuthenticator(
		[FromQuery] AccountController.VerifyGoogleAuthenticatorRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		bool isVerified = await googleAuthenticatorService.VerifyCode(
			authCode: user.AuthorizationCode,
			code: request.UserCode
		);
		return Ok(new AccountController.VerifyGoogleAuthenticatorResponse(IsVerified: isVerified));
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
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/activity/online")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
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
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/activity/offline")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
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
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/photo/upload")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(UploadProfilePhotoResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
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

	/// <summary>
	/// Заменяет текущий пароль пользователя на новый и завершает все сессии, кроме текущей
	/// </summary>
	/// <remarks>
	/// Пример запроса к API:
	///
	///     PUT api/user/profile/security/password/change
	///
	/// </remarks>
	/// <response code="200">Возвращает сообщение об успешной смене пароля</response>
	/// <response code="400">Пользователь пытается установить новый пароль, который одинаков с текущим</response>
	/// <response code="400">Текущий пароль пользователя указан неверно</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/security/password/change")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ChangePasswordResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
		[FromBody] ChangePasswordRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (request.CurrentPassword.Equals(request.NewPassword))
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Новый и текущий пароли совпадают.");

		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);
		bool isVerified = hashService.Verify(text: request.CurrentPassword, hashedText: user.Password);

		if (!isVerified)
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Текущий пароль указан неверно.");

		Session currentSession = await GetCurrentSession(cancellationToken: cancellationToken);

		context.Entry(entity: user).State = EntityState.Modified;
		user.Password = hashService.Generate(toHash: request.NewPassword);
		SessionActivityStatus disableStatus = await GetSessionActivityStatus(
			activityStatus: SessionActivityStatuses.Disable,
			cancellationToken: cancellationToken
		);

		foreach (Session session in user.Sessions.Except(second: new Session[] { currentSession }))
		{
			context.Entry(entity: session).State = EntityState.Modified;
			session.SessionActivityStatus = disableStatus;
		}

		await context.SaveChangesAsync(cancellationToken: cancellationToken);

		return Ok(value: new ChangePasswordResponse(Message: "Текущий пароль изменен успешно!"));
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
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpDelete(template: "profile/photo/delete")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
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
