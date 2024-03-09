using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.GoogleAuthenticator;
using MyJournal.API.Assets.Security.Hash;
using MyJournal.API.Assets.Security.JWT;
using MyJournal.API.Assets.Utilities;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/account")]
public class AccountController(
    MyJournalContext context,
    IHashService hash,
    IJwtService jwt,
    IGoogleAuthenticatorService googleAuthenticatorService
) : MyJournalBaseController(context: context)
{
    private readonly MyJournalContext _context = context;

    #region Records
    [Validator<VerifyRegistrationCodeRequestValidator>]
    public record VerifyRegistrationCodeRequest(string RegistrationCode);
    public record VerifyRegistrationCodeResponse(bool IsVerified);

    [Validator<SignInRequestValidator>]
    public record SignInRequest(string Login, string Password, Clients Client);
    public record SignInResponse(string Token);

    public record SignInWithTokenResponse(bool SessionIsEnabled);

    [Validator<SignUpRequestValidator>]
    public record SignUpRequest(string RegistrationCode, string Login, string Password);
    public record SignUpResponse(int Id);

    public record SignOutResponse(string Result);

    public record GetGoogleAuthenticatorResponse(string QrCodeBase64, string AuthenticationCode);

    [Validator<AccountControllerVerifyGoogleAuthenticatorRequestValidator>]
    public record VerifyGoogleAuthenticatorRequest(string UserCode);
    public record VerifyGoogleAuthenticatorResponse(bool IsVerified);

    [Validator<SetPhoneRequestValidator>]
    public record SetPhoneRequest(string NewPhone);
    public record SetPhoneResponse(string Message);

    [Validator<SetEmailRequestValidator>]
    public record SetEmailRequest(string NewEmail);
    public record SetEmailResponse(string Message);

    [Validator<VerifyPhoneRequestValidator>]
    public record VerifyPhoneRequest(string Phone);
    public record VerifyPhoneResponse(int UserId);

    [Validator<VerifyEmailRequestValidator>]
    public record VerifyEmailRequest(string Email);
    public record VerifyEmailResponse(int UserId);

    [Validator<ResetPasswordRequestValidator>]
    public record ResetPasswordRequest(string NewPassword);
    public record ResetPasswordResponse(string Message);
    #endregion

    #region Methods
    #region AuxiliaryMethods
    private async Task DisableSession(
        Session session,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        _context.Entry(entity: session).State = EntityState.Modified;
        session.SessionActivityStatus = await FindSessionActivityStatus(
            activityStatus: SessionActivityStatuses.Disable,
            cancellationToken: cancellationToken
        );
    }
    #endregion

    #region GET
    /// <summary>
    /// Проверка регистрационного кода на коррекность и принадлежность какому-либо пользователю
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	GET api/account/registration-code/verify?RegistrationCode=1234567
    ///
    /// Параметры:
    ///
    ///	RegistrationCode - регистрационный код, который выдается учебным заведением для дальнейшей регистрации пользователем в системе
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает статус переданного регистрационного кода: true - существует, false - не сушествует</response>
    /// <response code="404">Некорректный регистрационный код</response>
    [HttpGet(template: "registration-code/verify")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(VerifyRegistrationCodeResponse))]
    public async Task<ActionResult<VerifyRegistrationCodeResponse>> VerifyRegistrationCode(
        [FromQuery] VerifyRegistrationCodeRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User? user = await FindUserByRegistrationCodeAsync(
            registrationCode: request.RegistrationCode,
            cancellationToken: cancellationToken
        );

        return Ok(value: new VerifyRegistrationCodeResponse(
            IsVerified: user is not null && user.RegistrationCode!.Equals(request.RegistrationCode)
        ));
    }

    /// <summary>
    /// Получение данных для дальнейшего использования Google Authenticator
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	GET api/account/sign-up/user/{id:int}/code/get
    ///
    /// Параметры:
    ///
    ///	id - идентификатор пользователя, для которого необходимо получить данные от Google Authenticator
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает ссылку на QR-код в виде изображения и символьный код</response>
    /// <response code="404">Некорректный идентификатор пользователя</response>
    [HttpGet(template: "sign-up/user/{id:int}/code/get")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetGoogleAuthenticatorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    public async Task<ActionResult<GetGoogleAuthenticatorResponse>> GetGoogleAuthenticator(
        [FromRoute] int id,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор пользователя.");

        _context.Entry(entity: user).State = EntityState.Modified;
        user.AuthorizationCode = await googleAuthenticatorService.GenerateAuthenticationCode();
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        IGoogleAuthenticatorService.AuthenticationData data = await googleAuthenticatorService.GenerateQrCode(
            username: $"{user.Surname} {user.Name}",
            authCode: user.AuthorizationCode
        );
        return Ok(new GetGoogleAuthenticatorResponse(QrCodeBase64: data.QrCodeUrl, AuthenticationCode: data.Code));
    }

    /// <summary>
    /// Возвращает значение, является ли код из Google Authenticator, введенный пользователем, верным
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	GET api/account/user/{id:int}/code/verify?UserCode=123456
    ///
    /// Параметры:
    ///
    ///	id - идентификатор пользователя, для которого необходимо проверить код из Google Authenticator
    ///	UserCode - код из Google Authenticator, который необходимо проверить
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает статус кода от пользователя: true - верный, false - неверный</response>
    /// <response code="404">Некорректный идентификатор пользователя</response>
    [HttpGet(template: "user/{id:int}/code/verify")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(VerifyGoogleAuthenticatorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    public async Task<ActionResult<VerifyGoogleAuthenticatorResponse>> VerifyGoogleAuthenticator(
        [FromRoute] int id,
        [FromQuery] VerifyGoogleAuthenticatorRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
                    throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор пользователя.");

        bool isVerified = await googleAuthenticatorService.VerifyCode(
            authCode: user.AuthorizationCode,
            code: request.UserCode
        );
        return Ok(new VerifyGoogleAuthenticatorResponse(IsVerified: isVerified));
    }

    /// <summary>
    /// Получение идентификатора пользователя, к которому привязан номер телефона
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	GET api/account/restoring-access/phone/user/id/get?Phone=%2B7%28123%29456-7890`
    ///
    /// Параметры:
    ///
    ///	Phone - номер телефона, который привязан к аккаунту пользователя в формате +7(###)###-####
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает идентификатор пользователя с указанным номером телефона</response>
    /// <response code="404">Некорректный номер телефона</response>
    [HttpGet(template: "restoring-access/phone/user/id/get")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(VerifyPhoneResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
    public async Task<ActionResult<VerifyPhoneResponse>> GetPhoneOwner(
        [FromQuery] VerifyPhoneRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User user = await _context.Users.SingleOrDefaultAsync(
            predicate: user => user.Phone != null && user.Phone.Equals(request.Phone),
            cancellationToken: cancellationToken
        ) ?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный номер телефона.");
        return Ok(value: new VerifyPhoneResponse(UserId: user.Id));
    }

    /// <summary>
    /// Получение идентификатора пользователя, к которому привязан адрес электронной почты
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	GET api/account/restoring-access/email/user/id/get?Email=test%40mail.ru
    ///
    /// Параметры:
    ///
    ///	Email - адрес электронной почты, который привязан к аккаунту пользователя в формате address@example.com
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает идентификатор пользователя с указанным адресом электронной почты</response>
    /// <response code="404">Некорректный номер телефона</response>
    [HttpGet(template: "restoring-access/email/user/id/get")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(VerifyEmailResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
    public async Task<ActionResult<VerifyEmailResponse>> GetEmailOwner(
        [FromQuery] VerifyEmailRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User user = await _context.Users.SingleOrDefaultAsync(
            predicate: user => user.Email != null && user.Email.Equals(request.Email),
            cancellationToken: cancellationToken
        ) ?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный адрес электронной почты.");
        return Ok(value: new VerifyEmailResponse(UserId: user.Id));
    }
    #endregion

    #region POST
    /// <summary>
    /// Вход в аккаунт через логин и пароль
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-in/credentials
    ///	{
    ///		"Login": "login",
    ///		"Password": "password",
    ///		"Client": 0
    ///	}
    ///
    /// Параметры:
    ///
    ///	Login - логин, под которым пользователь будет использоваться для идентификации при аутентификации в процессе авторизации
    ///	Password - пароль, под которым пользователь будет авторизовываться в процессе авторизации
    ///	Client - идентификатор клиента, в котором пользователь проходит процесс авторизации:
    ///     0 - Windows
    ///     1 - Linux
    ///     2 - Chrome
    ///     3 - Opera
    ///     4 - Yandex
    ///     5 - Другой браузер
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает авторизационный токен, содержащий информацию о пользователе и текущей сессии</response>
    /// <response code="404">Авторизационные данные неверны</response>
    [HttpPost(template: "sign-in/credentials")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignInResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SignInResponse>> SignInWithCredentials(
        [FromBody] SignInRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User? user = await FindUserByLoginAsync(login: request.Login, cancellationToken: cancellationToken);

        if (user is null || !hash.Verify(text: request.Password, hashedText: user.Password))
            throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Неверный логин и/или пароль пользователя.");

        Session currentSession = new Session()
        {
            User = user,
            Ip = GetSenderIp().ToString(),
            MyJournalClient = await FindMyJournalClientByClientType(
                clientType: request.Client,
                cancellationToken: cancellationToken
            ),
            SessionActivityStatus = await FindSessionActivityStatus(
                activityStatus: SessionActivityStatuses.Enable,
                cancellationToken: cancellationToken
            )
        };

        await _context.Sessions.AddAsync(entity: currentSession, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        string token = jwt.Generate(tokenOwner: user, sessionId: currentSession.Id);
        return Ok(value: new SignInResponse(Token: token));
    }

    /// <summary>
    /// Вход в аккаунт через авторизационный токен, указанный в заголовках авторизации
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-in/token
    ///
    /// Параметры:
    ///
    ///	token - сохраненный авторизационный токен, помещенный в заголовок 'Authorization' http-запроса
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Возвращает статус текущей сессии: true, если сессия активна и false, если неактивна</response>
    /// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
    [Authorize]
    [HttpPost(template: "sign-in/token")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignInWithTokenResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SignInWithTokenResponse>> SignInWithToken(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Session session = await GetCurrentSession(cancellationToken: cancellationToken);

        return Ok(value: new SignInWithTokenResponse(
            SessionIsEnabled: session.SessionActivityStatus.ActivityStatus.Equals(SessionActivityStatuses.Enable)
        ));
    }

    /// <summary>
    /// Регистрация в системе через регистрационный код
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-up
    ///	{
    ///		"RegistrationCode": "1234567",
    ///		"Login": "login",
    ///		"Password": "password"
    ///	}
    ///
    /// Параметры:
    ///
    ///	RegistrationCode - регистрационный код, выданный учебной организацией, используемый для идентификации пользователя
    ///	Login - логин, который будет использоваться в дальнейшем для идентификации в процессе авторизации
    ///	Password - пароль, который будет использоваться в дальнейшем для аутентификации в процессе авторизации
    ///
    /// ]]>
    /// </remarks>
    /// <response code="204">Аккаунт успешно создан</response>
    /// <response code="400">Переданный логин занят другим пользователем</response>
    /// <response code="404">Неверный регистрационный код</response>
    [HttpPost(template: "sign-up")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignUpResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SignUpResponse>> SignUp(
        [FromBody] SignUpRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User? user = await FindUserByRegistrationCodeAsync(
            registrationCode: request.RegistrationCode,
            cancellationToken: cancellationToken
        );
        if (user is null)
            throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Неверный регистрационный код.");

        if (await FindUserByLoginAsync(login: request.Login, cancellationToken: cancellationToken) is not null)
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Данный логин не может быть занят.");

        _context.Entry(entity: user).State = EntityState.Modified;
        user.RegistrationCode = null;
        user.RegisteredAt = DateTime.Now;
        user.Login = request.Login;
        user.Password = hash.Generate(toHash: request.Password);

        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignUpResponse(Id: user.Id));
    }

    /// <summary>
    /// Завершение текущего сеанса
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-out/this
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Сессия успешно завершена</response>
    /// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
    [Authorize]
    [HttpPost(template: "sign-out/this")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SignOutResponse>> SignOutThis(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Session session = await GetCurrentSession(cancellationToken: cancellationToken);

        if (session.SessionActivityStatus.ActivityStatus.Equals(SessionActivityStatuses.Disable))
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанная сессия уже является неактивной.");

        await DisableSession(session: session, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignOutResponse(Result: "Текущая сессия успешно завершена."));
    }

    /// <summary>
    /// Завершение всех сеансов
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-out/all
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Все сессии успешно завершены</response>
    /// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
    [Authorize]
    [HttpPost(template: "sign-out/all")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SignOutResponse>> SignOutAll(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User user = await GetAuthorizedUser(cancellationToken: cancellationToken);

        foreach (Session session in user.Sessions)
            await DisableSession(session: session, cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignOutResponse(Result: "Все сессии успешно завершены."));
    }

    /// <summary>
    /// Завершение всех сеансов, кроме текущего
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-out/others
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Все сессии, кроме текущей, успешно завершены</response>
    /// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
    [Authorize]
    [HttpPost(template: "sign-out/others")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SignOutResponse>> SignOutOthers(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User user = await GetAuthorizedUser(cancellationToken: cancellationToken);
        Session currentSession = await GetCurrentSession(cancellationToken: cancellationToken);

        foreach (Session session in user.Sessions.Except(second: new Session[] { currentSession }))
            await DisableSession(session: session, cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignOutResponse(Result: "Все сессии, кроме текущей, успешно завершены."));
    }

    /// <summary>
    /// Устанавливает номер телефона пользователя
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-up/user/{id:int}/phone/set
    ///	{
    ///		"NewPhone": "+7(123)456-7890",
    ///	}
    ///
    /// Параметры:
    ///
    ///	id - идентификатор пользователя, к которому будет привязан номер телефона
    ///	NewPhone - номер телефона, который будет привязан к аккаунту пользователя в формате +7(###)###-####
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Номер телефона успешно установлен</response>
    /// <response code="400">Указанный номер телефона не может быть занят</response>
    /// <response code="400">Некорректный идентификатор пользователя</response>
    [HttpPost(template: "sign-up/user/{id:int}/phone/set")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SetPhoneResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SetPhoneResponse>> SetPhone(
        [FromBody] SetPhoneRequest request,
        [FromRoute] int id,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (await _context.Users.AnyAsync(predicate: user => user.Phone != null && user.Phone.Equals(request.NewPhone), cancellationToken: cancellationToken))
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанный номер телефона не может быть занят.");

        User user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор пользователя.");

        _context.Entry(entity: user).State = EntityState.Modified;
        user.Phone = request.NewPhone;
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SetPhoneResponse(Message: "Номер телефона успешно установлен!"));
    }

    /// <summary>
    /// Устанавливает электронную почту пользователя
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/sign-up/user/{id:int}/email/set
    ///	{
    ///		"NewEmail": "test@mail.ru",
    ///	}
    ///
    /// Параметры:
    ///
    ///	id - идентификатор пользователя, к которому будет привязан адрес электронной почты
    ///	NewEmail - новый адрес электронной почты, который будет привязан к аккаунту пользователя в формате address@example.com
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Электронная почта успешно установлена</response>
    /// <response code="400">Указанный адрес электронной почты не может быть занят</response>
    /// <response code="400">Некорректный идентификатор пользователя</response>
    [HttpPost(template: "sign-up/user/{id:int}/email/set")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SetEmailResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    public async Task<ActionResult<SetEmailResponse>> SetEmail(
        [FromRoute] int id,
        [FromBody] SetEmailRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (await _context.Users.AnyAsync(predicate: user => user.Email != null && user.Email.Equals(request.NewEmail), cancellationToken: cancellationToken))
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанный адрес электронной почты не может быть занят.");

        User user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор пользователя.");

        _context.Entry(entity: user).State = EntityState.Modified;
        user.Email = request.NewEmail;
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SetPhoneResponse(Message: "Электронная почта успешно установлена!"));
    }

    /// <summary>
    /// Устанавливает новый пароль для аутентификации пользователя
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Пример запроса к API:
    ///
    ///	POST api/account/restoring-access/user/{id:int}/password/reset
    ///	{
    ///		"NewPassword": "password",
    ///	}
    ///
    /// Параметры:
    ///
    ///	id - идентификатор пользователя, для которого будет установлен новый пароль
    ///	NewPassword - новый пароль от аккаунта пользователя, который будет использоваться для дальнейшей аутентификации в процессе авторизации
    ///
    /// ]]>
    /// </remarks>
    /// <response code="200">Смена пароля прошла успешно</response>
    /// <response code="400">Указанный пароль не может быть занят</response>
    /// <response code="400">Некорректный идентификатор пользователя</response>
    [HttpPost(template: "restoring-access/user/{id:int}/password/reset")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResetPasswordResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        [FromRoute] int id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        IQueryable<User> usersWithPassword = _context.Users.Where(predicate: user => !String.IsNullOrEmpty(user.Password));
        if (usersWithPassword.AsEnumerable().Any(predicate: user => hash.Verify(text: request.NewPassword, hashedText: user.Password)))
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанный пароль не может быть занят.");

        User user = await FindUserByIdAsync(id: id, cancellationToken: cancellationToken) ??
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный идентификатор пользователя.");

        _context.Entry(entity: user).State = EntityState.Modified;
        user.Password = hash.Generate(toHash: request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SetPhoneResponse(Message: "Новый пароль успешно установлен!"));
    }
    #endregion
    #endregion
}