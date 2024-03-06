namespace MyJournal.Core.Utilities.GoogleAuthenticatorService;

public interface IGoogleAuthenticatorService
{
	Task<bool> VerifyAuthenticationCode(
		int userId,
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}