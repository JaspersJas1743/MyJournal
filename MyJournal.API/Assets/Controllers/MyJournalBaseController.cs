using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Security.JWT;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API.Assets.Controllers;

public class MyJournalBaseController(
	MyJournalContext context
) : ControllerBase
{
	protected int GetAuthorizedUserId()
	{
		return Int32.Parse(s: HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Identifier)
							  ?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен."));
	}

	protected async Task<User> GetAuthorizedUser(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		User user = await context.Users
			.Include(navigationPropertyPath: user => user.Sessions)
			.ThenInclude(navigationPropertyPath: session => session.SessionActivityStatus)
			.SingleOrDefaultAsync(
				predicate: user => user.Id.Equals(userId),
				cancellationToken: cancellationToken
			) ?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		return user;
	}

	protected int GetCurrentSessionId()
	{
		return Int32.Parse(s: HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Session)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен."));
	}

	protected async Task<Session> GetCurrentSession(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int sessionId = GetCurrentSessionId();
		Session session = await context.Sessions.Include(navigationPropertyPath: session => session.SessionActivityStatus)
			.SingleOrDefaultAsync(
				predicate: session => session.Id.Equals(sessionId),
				cancellationToken: cancellationToken
			) ?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		return session;
	}

	protected IPAddress GetSenderIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4() ??
               throw new NullReferenceException(message: "HttpContext.Connection.RemoteIpAddress is null");
    }

	protected async Task<User?> FindUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        IQueryable<User> users = context.Users.Include(navigationPropertyPath: user => user.UserRole);

        return await users.SingleOrDefaultAsync(
            predicate: user => user.Id.Equals(id),
            cancellationToken: cancellationToken
        );
    }

	protected async Task<User?> FindUserByLoginAsync(
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

	protected async Task<User?> FindUserByRegistrationCodeAsync(
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

	protected async Task<MyJournalClient> FindMyJournalClientByClientType(
        Clients clientType,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        return await context.MyJournalClients.FirstAsync(
            predicate: client => client.Client.ClientName.Equals(clientType),
            cancellationToken: cancellationToken
        );
    }

    protected async Task<SessionActivityStatus> FindSessionActivityStatus(
        SessionActivityStatuses activityStatus,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        return await context.SessionActivityStatuses.FirstAsync(
            predicate: status => status.ActivityStatus.Equals(activityStatus),
            cancellationToken: cancellationToken
        );
    }

	protected async Task<UserActivityStatus> FindUserActivityStatus(
		UserActivityStatuses activityStatus,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await context.UserActivityStatuses.FirstAsync(
			predicate: status => status.ActivityStatus.Equals(activityStatus),
			cancellationToken: cancellationToken
		);
	}

	protected async Task<IEnumerable<string>> GetUserInterlocutorIds(
		int userId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await context.Users
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Users)
			.SingleAsync(predicate: u => u.Id.Equals(userId), cancellationToken: cancellationToken);
		return user.Chats.SelectMany(selector: c => c.Users.Except(second: new User[] { user })).Select(selector: u => u.Id.ToString());
	}

	protected async Task<IEnumerable<Session>> GetOtherSessionIds(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);
		Session currentSession = await GetCurrentSession(cancellationToken: cancellationToken);

		IEnumerable<Session> sessionsToDisable = user.Sessions
			.Where(predicate: s => s.SessionActivityStatus.ActivityStatus == SessionActivityStatuses.Enable)
			.Except(second: new Session[] { currentSession });

		return sessionsToDisable;
	}
}