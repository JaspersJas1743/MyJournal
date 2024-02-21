using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public interface IRegistrationService<T>
{
	Task<bool> Register(
		Credentials<T> credentials,
		IVerificationService<Credentials<T>>? verifier = default(IVerificationService<Credentials<T>>),
		CancellationToken cancellationToken = default(CancellationToken)
	);
}