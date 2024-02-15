using System.Net;
using MyJournal.API.Assets.DatabaseModels;

namespace MyJournal.API.Assets.Security.JWT;

public interface IJwtService
{
	string Generate(User tokenOwner, IPAddress tokenOwnerIp, Clients usedClient);
}