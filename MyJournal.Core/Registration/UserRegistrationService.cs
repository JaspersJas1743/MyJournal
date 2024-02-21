using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public sealed class UserRegistrationService : IRegistrationService<User>
{
	public async Task<bool> Register(
		Credentials<User> credentials,
		IVerificationService<Credentials<User>>? verifier = default(IVerificationService<Credentials<User>>),
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (verifier is not null)
		{
			if (!await verifier.Verify(toVerifying: credentials, cancellationToken: cancellationToken))
				return false;
		}

		try
		{
			await ApiClient.PostAsync(
				apiMethod: "Account/SignUp",
				arg: credentials,
				cancellationToken: cancellationToken
			);

			return true;
		}
		catch (Exception e)
		{
			return false;
		}
	}
}