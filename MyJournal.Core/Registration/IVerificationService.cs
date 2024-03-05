namespace MyJournal.Core.Registration;

public interface IVerificationService<in T>
{
	Task<bool> Verify(
		T toVerifying,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}