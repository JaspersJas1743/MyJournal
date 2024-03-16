using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MyJournal.API.Assets.Security.JWT;

namespace MyJournal.API.Assets.Hubs;

public sealed class UserIdProvider : IUserIdProvider
{
	public string? GetUserId(HubConnectionContext connection)
		=> connection.User.FindFirstValue(claimType: MyJournalClaimTypes.Identifier);
}