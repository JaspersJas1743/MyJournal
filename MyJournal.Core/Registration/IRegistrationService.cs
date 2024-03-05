using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public interface IRegistrationService<T>
{
	public record AuthenticationData(string QrCodeBase64, string AuthenticationCode);

	Task<bool> Register(
		Credentials<T> credentials,
		IVerificationService<Credentials<T>>? verifier = default(IVerificationService<Credentials<T>>),
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task<AuthenticationData> CreateGoogleAuthenticator(
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task<bool> VerifyAuthenticationCode(
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task SetEmail(
		string email,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task SetPhone(
		string phone,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}