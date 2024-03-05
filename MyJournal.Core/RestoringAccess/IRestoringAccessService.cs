using MyJournal.Core.Utilities;

namespace MyJournal.Core.RestoringAccess;

public interface IRestoringAccessService<T>
{
	Task<bool> VerifyCredential(
		Credentials<T> credentials,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task<bool> VerifyAuthenticationCode(
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task ResetPassword(
		string newPassword,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}