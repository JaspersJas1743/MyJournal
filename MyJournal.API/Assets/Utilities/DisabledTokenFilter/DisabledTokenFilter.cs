using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Security.JWT;

namespace MyJournal.API.Assets.Utilities.DisabledTokenFilter;

public sealed class DisabledTokenFilter(MyJournalContext dbContext) : IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context)
	{
		string token = context.HttpContext.Request.Headers.Authorization.ToString();
		if (!token.Contains(value: JwtBearerDefaults.AuthenticationScheme))
			return;

		int sessionId = Int32.Parse(s: context.HttpContext.User.FindFirstValue(claimType: MyJournalClaimTypes.Session)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен."));
		Session session = dbContext.Sessions.Include(navigationPropertyPath: session => session.SessionActivityStatus)
			.SingleOrDefault(predicate: session => session.Id.Equals(sessionId))
			?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		if (session.SessionActivityStatus.ActivityStatus.Equals(SessionActivityStatuses.Enable))
			return;

		context.Result = new DisabledTokenResult(message: "Данная сессия была завершена.");
	}

	public void OnActionExecuted(ActionExecutedContext context) { }
}

public static class DisabledTokenFilterExtension
{
	public static MvcOptions AddDisabledTokenFilter(this MvcOptions options)
	{
		options.Filters.Add<DisabledTokenFilter>();
		return options;
	}
}