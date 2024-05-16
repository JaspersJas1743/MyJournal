using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.GoogleAuthenticator;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.Security.Hash;
using MyJournal.API.Assets.Utilities;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Controllers;

[Authorize]
[ApiController]
[Route(template: "api/user")]
public sealed class UserController(
	MyJournalContext context,
	IHubContext<UserHub, IUserHub> userHubContext,
	IHashService hashService,
	IGoogleAuthenticatorService googleAuthenticatorService
) : MyJournalBaseController(context: context)
{
	#region Fields
	private readonly MyJournalContext _context = context;
	#endregion

	#region Records
	public record GetInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);

	public record GetUserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Photo, UserActivityStatuses Activity, DateTime? OnlineAt);

	[Validator<UserControllerVerifyGoogleAuthenticatorRequest>]
	public record VerifyGoogleAuthenticatorRequest(string UserCode);
	public record UserControllerVerifyGoogleAuthenticatorResponse(bool IsVerified);

	[Validator<UploadProfilePhotoRequestValidator>]
	public record UploadProfilePhotoRequest(string Link);
	public record UploadProfilePhotoResponse(string Message);

	[Validator<ChangePasswordRequestValidator>]
	public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
	public record ChangePasswordResponse(string Message);

	[Validator<ChangeEmailRequestValidator>]
	public record ChangeEmailRequest(string NewEmail);
	public record ChangeEmailResponse(string Email, string Message);

	[Validator<ChangePhoneRequestValidator>]
	public record ChangePhoneRequest(string NewPhone);
	public record ChangePhoneResponse(string Phone, string Message);
	#endregion

	#region Methods
	#region AuxiliaryMethods
	private async Task<(int id, DateTime? onlineAt)> SetActivityStatus(
		UserActivityStatuses activityStatus,
		DateTime? onlineAt,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		user.UserActivityStatus = await FindUserActivityStatus(
			activityStatus: activityStatus,
			cancellationToken: cancellationToken
		);
		user.OnlineAt = onlineAt;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		return (user.Id, onlineAt);
	}

	private async Task DisableSessions(
		IQueryable<Session> sessions,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		SessionActivityStatus disableStatus = await FindSessionActivityStatus(
			activityStatus: SessionActivityStatuses.Disable,
			cancellationToken: cancellationToken
		);
		foreach (Session session in sessions)
			session.SessionActivityStatus = disableStatus;
	}
	#endregion

	#region GET
	/// <summary>
	/// Получение основной информации
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/user/profile/info/me
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Основная информацию об авторизованном пользователе</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "profile/info/me")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetInformationResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetInformationResponse>> GetInformation(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		return Ok(value: new GetInformationResponse(
			Id: user.Id,
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
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/user/profile/info/{id:int}
	///
	/// Параметры:
	///
	///	id - идентификатор пользователя, информацию о котором необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Основная информация о пользователе</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "profile/info/{id:int}")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetUserInformationResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetUserInformationResponse>> GetUserInformation(
		[FromRoute] int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
					 throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный идентификатор пользователя.");

		return Ok(value: new GetUserInformationResponse(
			Id: user.Id,
			Surname: user.Surname,
			Name: user.Name,
			Patronymic: user.Patronymic,
			Activity: user.UserActivityStatus.ActivityStatus,
			OnlineAt: user.OnlineAt,
			Photo: user.LinkToPhoto
		));
	}

	/// <summary>
	/// Возвращает значение, является ли код из Google Authenticator, введенный пользователем, верным
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/user/profile/security/code/verify?UserCode=123456
	///
	/// Параметры:
	///
	///	UserCode - код из Google Authenticator, который необходимо проверить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Статус корректности кода: true - верный, false - неверный</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "profile/security/code/verify")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(UserControllerVerifyGoogleAuthenticatorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<UserControllerVerifyGoogleAuthenticatorResponse>> VerifyGoogleAuthenticator(
		[FromQuery] VerifyGoogleAuthenticatorRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		bool isVerified = await googleAuthenticatorService.VerifyCode(
			authCode: user.AuthorizationCode,
			code: request.UserCode
		);
		return Ok(new UserControllerVerifyGoogleAuthenticatorResponse(IsVerified: isVerified));
	}
	#endregion

	#region PUT
	/// <summary>
	/// Изменяет статус активности на "В сети"
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/user/profile/activity/online
	///
	/// ]]>
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
		await userHubContext.Clients.Users(
			userIds: await GetUserInterlocutorIds(userId: userId, cancellationToken: cancellationToken)
		).SetOnline(userId: userId, onlineAt: onlineAt);
		return Ok();
	}

	/// <summary>
	/// Изменяет статус активности на "Не в сети"
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/user/profile/activity/offline
	///
	/// ]]>
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
			onlineAt: DateTime.Now.AddHours(value: 3),
			cancellationToken: cancellationToken
		);
		await userHubContext.Clients.Users(
			userIds: await GetUserInterlocutorIds(userId: userId, cancellationToken: cancellationToken)
		).SetOffline(userId: userId, onlineAt: onlineAt);
		return Ok();
	}

	/// <summary>
	/// Установка фотографии профиля
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/user/profile/photo/upload
	///
	/// Параметры:
	///
	///	Photo - фотография, которая будет установлена в качестве аватара пользователя
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Ссылка на фотографию профиля пользователя</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/photo/upload")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(UploadProfilePhotoResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<UploadProfilePhotoResponse>> UploadProfilePhoto(
		[FromQuery] UploadProfilePhotoRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		user.LinkToPhoto = request.Link;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.Users(
			userIds: await GetUserInterlocutorIds(userId: user.Id, cancellationToken: cancellationToken)
		).UpdatedProfilePhoto(userId: user.Id, link: request.Link);

		return Ok(value: new UploadProfilePhotoResponse(Message: "Фотография изменена успешно!"));
	}

	/// <summary>
	/// Заменяет текущий пароль пользователя на новый и завершает все сессии, кроме текущей
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/user/profile/security/password/change
	///	{
	///		"CurrentPassword": "currentPassword",
	///		"NewPassword": "newPassword"
	///	}
	///
	/// Параметры:
	///
	///	CurrentPassword - текущий пароль пользователя, который использовался для для аутентификации в процессе авторизации
	///	NewPassword - новый пароль от аккаунта пользователя, который будет использоваться для дальнейшей аутентификации в процессе авторизации
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Успешная смена пароля</response>
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
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);
		bool isVerified = hashService.Verify(text: request.CurrentPassword, hashedText: user.Password);

		if (!isVerified)
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Текущий пароль указан неверно.");

		user.Password = hashService.Generate(toHash: request.NewPassword);

		int currentSessionId = GetCurrentSessionId();
		IQueryable<Session> sessionsToDisable = _context.Users
			.Where(u => u.Id.Equals(user.Id))
			.SelectMany(u => u.Sessions.Where(
				s => s.SessionActivityStatus.ActivityStatus == SessionActivityStatuses.Enable && !s.Id.Equals(currentSessionId)
			));

		List<int> sessionIds = sessionsToDisable.Select(s => s.Id).ToList();

		await DisableSessions(sessions: sessionsToDisable, cancellationToken: cancellationToken);
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);
		await userHubContext.Clients.User(userId: user.Id.ToString()).SignOut(sessionIds: sessionIds);

		return Ok(value: new ChangePasswordResponse(Message: "Текущий пароль изменен успешно!"));
	}

	/// <summary>
	/// Заменяет текущий адрес электронной почты пользователя на новый
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/user/profile/security/email/change
	///	{
	///		"NewEmail": "test@mail.ru"
	///	}
	///
	/// Параметры:
	///
	///	NewEmail - новый адрес электронной почты, который будет привязан к аккаунту пользователя в формате address@example.com
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Успешная смена адреса электронной почты</response>
	/// <response code="400">Указанный адрес электронной почты занят другим пользователем</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/security/email/change")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ChangeEmailResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<ChangeEmailResponse>> ChangeEmail(
		[FromBody] ChangeEmailRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		if (await _context.Users.AnyAsync(predicate: u => u.Email != null && u.Email.Equals(request.NewEmail), cancellationToken: cancellationToken))
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанный адрес электронной почты не может быть занят.");

		user.Email = request.NewEmail;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.User(userId: user.Id.ToString()).SetEmail(email: user.Email);

		return Ok(value: new ChangeEmailResponse(Email: user.Email, Message: "Текущий адрес электронной почты изменен успешно!"));
	}

	/// <summary>
	/// Заменяет текущий номер телефона пользователя на новый
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/user/profile/security/phone/change
	///	{
	///		"NewPhone": "+7(123)456-7890"
	///	}
	///
	/// Параметры:
	///
	///	NewPhone - новый номер телефона, который будет привязан к аккаунту пользователя в формате +7(###)###-####
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Успешная смена номера телефона</response>
	/// <response code="400">Указанный номер телефона занят другим пользователем</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "profile/security/phone/change")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ChangePhoneResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<ChangePhoneResponse>> ChangePhone(
		[FromBody] ChangePhoneRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

		if (await _context.Users.AnyAsync(predicate: u => u.Phone != null && u.Phone.Equals(request.NewPhone), cancellationToken: cancellationToken))
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанный номер телефона не может быть занят.");

		user.Phone = request.NewPhone;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.User(userId: user.Id.ToString()).SetPhone(phone: user.Phone);

		return Ok(value: new ChangePhoneResponse(Phone: user.Phone, Message: "Текущий номер телефона изменен успешно!"));
	}
	#endregion

	#region DELETE
	/// <summary>
	/// Удаление фотографии профиля
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	DELETE api/user/profile/photo/delete
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Фотография профиля пользователя удалена успешно</response>
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

		user.LinkToPhoto = null;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.Users(
			userIds: await GetUserInterlocutorIds(userId: user.Id, cancellationToken: cancellationToken)
		).DeletedProfilePhoto(userId: user.Id);

		return Ok();
	}
	#endregion
	#endregion
}
