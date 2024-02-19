using MyJournal.API.Assets.DatabaseModels;

namespace MyJournal.API.Assets.Security.JWT;

public interface IJwtService
{
	string Generate(User tokenOwner, int sessionId);
}