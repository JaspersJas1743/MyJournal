namespace MyJournal.Core.Authorization;

public interface IAuthorizationService
{
	Task<User> SignIn(CancellationToken cancellationToken = default(CancellationToken));
}