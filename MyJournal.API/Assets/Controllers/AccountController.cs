using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Security.Hash;
using MyJournal.API.Assets.Security.JWT;
using MyJournal.API.Assets.Utilities;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Controllers
{
    [ApiController]
    [Route(template: "api/[controller]")]
    public class AccountController(MyJournalContext context, IHashService hash, IJwtService jwt) : ControllerBase
    {
        #region Records
        [Validator<SignInRequestValidator>]
        public record SignInRequest(string Login, string Password, Clients Client);
        public record SignInResponse(string Token);

        public record SignInWithTokenResponse(bool SessionIsEnabled);

        [Validator<SignUpRequestValidator>]
        public record SignUpRequest(string RegistrationCode, string Login, string Password);

        public record SignOutResponse(string Result);
        #endregion

        #region Methods
        private IPAddress GetSenderIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4()
                   ?? throw new NullReferenceException(message: "HttpContext.Connection.RemoteIpAddress is null");
        }

        private async Task<User?> FindUserByLoginAsync(
            string login,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            IQueryable<User> users = context.Users
                                            .Include(navigationPropertyPath: user => user.UserRole)
                                            .Where(predicate: user => !String.IsNullOrEmpty(user.Login));

            return await users.SingleOrDefaultAsync(predicate: user => user.Login!.Equals(login), cancellationToken: cancellationToken);
        }

        private async Task<User?> FindUserByRegistrationCodeAsync(
            string registrationCode,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            IQueryable<User> users = context.Users
                                            .Include(navigationPropertyPath: user => user.UserRole)
                                            .Where(predicate: user => !String.IsNullOrEmpty(user.RegistrationCode));

            return await users.SingleOrDefaultAsync(predicate: user => user.RegistrationCode!.Equals(registrationCode), cancellationToken: cancellationToken);
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

        private async Task<User?> GetAuthorizedUser(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            int userId = Int32.Parse(
                s: HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Identifier)
                   ?? throw new ArgumentNullException(message: "Некорректный авторизационный токен", paramName: nameof(MyJournalClaimTypes.Identifier))
            );
            User? user = await context.Users
                                     .Include(navigationPropertyPath: user => user.Sessions)
                                     .ThenInclude(navigationPropertyPath: session => session.SessionActivityStatus)
                                     .SingleOrDefaultAsync(predicate: user => user.Id.Equals(userId), cancellationToken: cancellationToken);
            return user;
        }

        private async Task<Session?> GetCurrentSession(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            int sessionId = Int32.Parse(
                s: HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Session)
                   ?? throw new ArgumentNullException(message: "Некорректный авторизационный токен", paramName: nameof(MyJournalClaimTypes.Session))
            );
            Session? session = await context.Sessions
                                           .Include(navigationPropertyPath: session => session.SessionActivityStatus)
                                           .SingleOrDefaultAsync(predicate: session => session.Id.Equals(sessionId), cancellationToken: cancellationToken);
            return session;
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

        #region POST
        /// <summary>
        /// Вход в аккаунт через логин и пароль
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /SignIn
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
        [HttpPost(template: nameof(SignIn))]
        [Produces(contentType: "application/json")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignInResponse))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ApiError))]
        public async Task<ActionResult<SignInResponse>> SignIn(
            [FromBody] SignInRequest request,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            User? user = await FindUserByLoginAsync(login: request.Login, cancellationToken: cancellationToken);

            if (user is null || !hash.Verify(text: request.Password, hashedText: user.Password))
                return NotFound(value: ApiError.Create(message: "Неверный логин и/или пароль пользователя."));

            Session currentSession = new Session()
            {
                User = user,
                Ip = GetSenderIp().ToString(),
                MyJournalClient = await FindMyJournalClientByClientType(clientType: request.Client, cancellationToken: cancellationToken),
                SessionActivityStatus = await GetActivityStatus(activityStatus: SessionActivityStatuses.Enable, cancellationToken: cancellationToken),
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
        /// Sample request:
        ///
        ///     POST /SignInWithToken
        ///
        /// </remarks>
        /// <response code="200">Возвращает статус текущей сессии: true, если сессия активна и false, если неактивна</response>
        /// <response code="400">Некорректный авторизационный токен</response>
        /// <response code="401">Пользователь не авторизован</response>
        [Authorize]
        [Produces(contentType: "application/json")]
        [HttpPost(template: nameof(SignInWithToken))]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignInWithTokenResponse))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ApiError))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
        public async Task<ActionResult<SignInWithTokenResponse>> SignInWithToken(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            Session? session = await GetCurrentSession(cancellationToken: cancellationToken);
            if (session is null)
                return BadRequest(error: ApiError.Create(message: "Некорректный авторизационный токен"));

            return Ok(value: new SignInWithTokenResponse(
                SessionIsEnabled: session.SessionActivityStatus.ActivityStatus.Equals(SessionActivityStatuses.Enable)
            ));
        }

        /// <summary>
        /// Регистрация в системе через регистрационный код
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /SignUp
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
        [HttpPost(template: nameof(SignUp))]
        [Produces(contentType: "application/json")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ApiError))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ApiError))]
        public async Task<ActionResult> SignUp(
            [FromBody] SignUpRequest request,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            User? user = await FindUserByRegistrationCodeAsync(registrationCode: request.RegistrationCode, cancellationToken: cancellationToken);
            if (user is null)
                return NotFound(value: ApiError.Create(message: "Неверный регистрационный код"));

            if (await FindUserByLoginAsync(login: request.Login, cancellationToken: cancellationToken) is not null)
                return BadRequest(error: ApiError.Create(message: "Данный логин не может быть занят"));

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
        /// Sample request:
        ///
        ///     POST /SignOut
        ///
        /// </remarks>
        /// <response code="200">Сессия успешно завершена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="400">Неверный авторизационный токен</response>
        [Authorize]
        [HttpPost(template: nameof(SignOut))]
        [Produces(contentType: "application/json")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ApiError))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
        public async Task<ActionResult<SignOutResponse>> SignOut(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            Session? session = await GetCurrentSession(cancellationToken: cancellationToken);
            if (session is null)
                return BadRequest(error: ApiError.Create(message: "Некорректный авторизационный токен"));

            await DisableSession(session: session, cancellationToken: cancellationToken);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return Ok(value: new SignOutResponse(Result: "Текущая сессия успешно завершена"));
        }

        /// <summary>
        /// Завершение всех сеансов
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /SignOutAll
        ///
        /// </remarks>
        /// <response code="200">Все сессии успешно завершены</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="400">Некорректный авторизационный токен</response>
        [Authorize]
        [HttpPost(template: nameof(SignOutAll))]
        [Produces(contentType: "application/json")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ApiError))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
        public async Task<ActionResult<SignOutResponse>> SignOutAll(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            User? user = await GetAuthorizedUser(cancellationToken: cancellationToken);
            if (user is null)
                return BadRequest(error: ApiError.Create(message: "Некорректный авторизационный токен"));

            foreach (Session session in user.Sessions)
                await DisableSession(session: session, cancellationToken: cancellationToken);

            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return Ok(value: new SignOutResponse(Result: "Все сессии успешно завершены"));
        }

        /// <summary>
        /// Завершение всех сеансов, кроме текущего
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /SignOutAllExceptThis
        ///
        /// </remarks>
        /// <response code="200">Все сессии, кроме текущей, успешно завершены</response>
        /// <response code="400">Некорректный авторизационный токен</response>
        /// <response code="401">Пользователь не авторизован</response>
        [Authorize]
        [Produces(contentType: "application/json")]
        [HttpPost(template: nameof(SignOutAllExceptThis))]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SignOutResponse))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ApiError))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(void))]
        public async Task<ActionResult<SignOutResponse>> SignOutAllExceptThis(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            User? user = await GetAuthorizedUser(cancellationToken: cancellationToken);
            Session? currentSession = await GetCurrentSession(cancellationToken: cancellationToken);

            if (user is null || currentSession is null)
                return BadRequest(error: ApiError.Create(message: "Некорректный авторизационный токен"));

            foreach (Session session in user.Sessions.Except(second: new Session[]{ currentSession }))
                await DisableSession(session: session, cancellationToken: cancellationToken);

            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return Ok(value: new SignOutResponse(Result: "Все сессии, кроме текущей, успешно завершены"));
        }
        #endregion
        #endregion
    }
}
