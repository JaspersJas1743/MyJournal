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
	protected async Task<User> GetAuthorizedUser(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = Int32.Parse(s: HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Identifier)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен."));
		User user = await context.Users.Include(navigationPropertyPath: user => user.Sessions)
			.ThenInclude(navigationPropertyPath: session => session.SessionActivityStatus)
			.SingleOrDefaultAsync(
				predicate: user => user.Id.Equals(userId),
				cancellationToken: cancellationToken
			) ?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		return user;
	}

	protected async Task<Session> GetCurrentSession(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int sessionId = Int32.Parse(s: HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Session)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен."));
		Session session = await context.Sessions.Include(navigationPropertyPath: session => session.SessionActivityStatus)
			.SingleOrDefaultAsync(
				predicate: session => session.Id.Equals(sessionId),
				cancellationToken: cancellationToken
			) ?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		return session;
	}
}