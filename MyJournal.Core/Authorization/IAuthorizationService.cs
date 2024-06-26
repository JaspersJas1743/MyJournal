using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public interface IAuthorizationService<T>
{
	Task<Authorized<T>> SignIn(Credentials<T> credentials, CancellationToken cancellationToken = default(CancellationToken));
}