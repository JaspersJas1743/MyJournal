using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
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
    IJwtService jwt
) : MyJournalBaseController(context: context)
{
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
    public record SignOutResponse(string Result);
    #endregion

    #region Methods
    #region AuxiliaryMethods
    private IPAddress GetSenderIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4() ??
               throw new NullReferenceException(message: "HttpContext.Connection.RemoteIpAddress is null");
    }

    private async Task<User?> FindUserByLoginAsync(
        string login,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        IQueryable<User> users = context.Users.Include(navigationPropertyPath: user => user.UserRole)
                                        .Where(predicate: user => !String.IsNullOrEmpty(user.Login));

        return await users.SingleOrDefaultAsync(
            predicate: user => user.Login!.Equals(login),
            cancellationToken: cancellationToken
        );
    }

    private async Task<User?> FindUserByRegistrationCodeAsync(
        string registrationCode,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        IQueryable<User> users = context.Users.Include(navigationPropertyPath: user => user.UserRole)
                                        .Where(predicate: user => !String.IsNullOrEmpty(user.RegistrationCode));

        return await users.SingleOrDefaultAsync(
            predicate: user => user.RegistrationCode!.Equals(registrationCode),
            cancellationToken: cancellationToken
        );
    }

    private async Task<MyJournalClient> FindMyJournalClientByClientType(
        Clients clientType,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        return await context.MyJournalClients.FirstAsync(
            predicate: client => client.Client.ClientName.Equals(clientType),
            cancellationToken: cancellationToken
        );
    }

    private async Task<SessionActivityStatus> GetActivityStatus(
        SessionActivityStatuses activityStatus,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        return await context.SessionActivityStatuses.FirstAsync(
            predicate: status => status.ActivityStatus.Equals(activityStatus),
            cancellationToken: cancellationToken
        );
    }

    private async Task DisableSession(
        Session session,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        context.Entry(entity: session).State = EntityState.Modified;
        session.SessionActivityStatus = await GetActivityStatus(
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
    /// Пример запроса к API:
    ///
    ///     GET api/account/code/verify?RegistrationCode=`your_code`
    ///
    /// </remarks>
    /// <response code="200">Возвращает статус переданного регистрационного кода: true - существует, false - не сушествует</response>
    /// <response code="404">Некорректный регистрационный код</response>
    [HttpGet(template: "code/verify")]
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
    #endregion

    #region POST
    /// <summary>
    /// Вход в аккаунт через логин и пароль
    /// </summary>
    /// <remarks>
    /// Пример запроса к API:
    ///
    ///     POST /api/account/sign-in/credentials
    ///     {
    ///        "Login": "your_login",
    ///        "Password": "your_password",
    ///        "Client": 0 - Windows
    ///                  1 - Linux
    ///                  2 - Chrome
    ///                  3 - Opera
    ///                  4 - Yandex
    ///                  5 - Other
    ///     }
    ///
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
            SessionActivityStatus = await GetActivityStatus(
                activityStatus: SessionActivityStatuses.Enable,
                cancellationToken: cancellationToken
            )
        };

        await context.Sessions.AddAsync(entity: currentSession, cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken: cancellationToken);

        string token = jwt.Generate(tokenOwner: user, sessionId: currentSession.Id);
        return Ok(value: new SignInResponse(Token: token));
    }

    /// <summary>
    /// Вход в аккаунт через авторизационный токен, указанный в заголовках авторизации
    /// </summary>
    /// <remarks>
    /// Пример запроса к API:
    ///
    ///     POST /api/account/sign-in/token
    ///
    /// </remarks>
    /// <response code="200">Возвращает статус текущей сессии: true, если сессия активна и false, если неактивна</response>
    /// <response code="400">Некорректный авторизационный токен</response>
    /// <response code="401">Пользователь не авторизован</response>
    [HttpPost(template: "sign-in/token")]
    [Authorize]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignInWithTokenResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
    public async Task<ActionResult<SignInWithTokenResponse>> SignInWithToken(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Session? session = await GetCurrentSession(cancellationToken: cancellationToken);
        if (session is null)
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

        return Ok(value: new SignInWithTokenResponse(
            SessionIsEnabled: session.SessionActivityStatus.ActivityStatus.Equals(SessionActivityStatuses.Enable)
        ));
    }

    /// <summary>
    /// Регистрация в системе через регистрационный код
    /// </summary>
    /// <remarks>
    /// Пример запроса к API:
    ///
    ///     POST /api/account/sign-up/
    ///     {
    ///        "RegistrationCode": "your_registration_code",
    ///        "Login": "login_for_auth",
    ///        "Password": "password_for_auth"
    ///     }
    ///
    /// </remarks>
    /// <response code="204">Аккаунт успешно создан</response>
    /// <response code="400">Переданный логин занят другим пользователем</response>
    /// <response code="404">Неверный регистрационный код</response>
    [HttpPost(template: "sign-up")]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
    public async Task<ActionResult> SignUp(
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

        context.Entry(entity: user).State = EntityState.Modified;
        user.RegistrationCode = null;
        user.RegisteredAt = DateTime.Now;
        user.Login = request.Login;
        user.Password = hash.Generate(toHash: request.Password);

        await context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Created();
    }

    /// <summary>
    /// Завершение текущего сеанса
    /// </summary>
    /// <remarks>
    /// Пример запроса к API:
    ///
    ///     POST /api/account/sign-out/this
    ///
    /// </remarks>
    /// <response code="200">Сессия успешно завершена</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="400">Неверный авторизационный токен</response>
    [HttpPost(template: "sign-out/this")]
    [Authorize]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
    public async Task<ActionResult<SignOutResponse>> SignOutThis(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Session? session = await GetCurrentSession(cancellationToken: cancellationToken);
        if (session is null)
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

        if (session.SessionActivityStatus.ActivityStatus.Equals(SessionActivityStatuses.Disable))
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Указанная сессия уже является неактивной.");

        await DisableSession(session: session, cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignOutResponse(Result: "Текущая сессия успешно завершена."));
    }

    /// <summary>
    /// Завершение всех сеансов
    /// </summary>
    /// <remarks>
    /// Пример запроса к API:
    ///
    ///     POST /api/account/sign-out/all
    ///
    /// </remarks>
    /// <response code="200">Все сессии успешно завершены</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="400">Некорректный авторизационный токен</response>
    [HttpPost(template: "sign-out/all")]
    [Authorize]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
    public async Task<ActionResult<SignOutResponse>> SignOutAll(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User? user = await GetAuthorizedUser(cancellationToken: cancellationToken);
        if (user is null)
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

        foreach (Session session in user.Sessions)
            await DisableSession(session: session, cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignOutResponse(Result: "Все сессии успешно завершены."));
    }

    /// <summary>
    /// Завершение всех сеансов, кроме текущего
    /// </summary>
    /// <remarks>
    /// Пример запроса к API:
    ///
    ///     POST /api/account/sign-out/others
    ///
    /// </remarks>
    /// <response code="200">Все сессии, кроме текущей, успешно завершены</response>
    /// <response code="400">Некорректный авторизационный токен</response>
    /// <response code="401">Пользователь не авторизован</response>
    [HttpPost(template: "sign-out/others")]
    [Authorize]
    [Produces(contentType: MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
    public async Task<ActionResult<SignOutResponse>> SignOutOthers(
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        User? user = await GetAuthorizedUser(cancellationToken: cancellationToken);
        Session? currentSession = await GetCurrentSession(cancellationToken: cancellationToken);

        if (user is null || currentSession is null)
            throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Некорректный авторизационный токен.");

        foreach (Session session in user.Sessions.Except(second: new Session[] { currentSession }))
            await DisableSession(session: session, cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken: cancellationToken);
        return Ok(value: new SignOutResponse(Result: "Все сессии, кроме текущей, успешно завершены."));
    }
    #endregion
    #endregion
}