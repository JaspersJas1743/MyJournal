using MyJournal.Core.Registration;
using MyJournal.Core.Utilities;

namespace MyJournal.Tests;

public class VerificationTests
{
	[Test]
	public async Task RegistrationCodeVerification_WithCorrectVerificationCode_ShouldReturnTrue()
	{
		RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			RegistrationCode = "testtes"
		};
		bool isVerified = await registrationCodeVerificationService.Verify(credentials: userCredentials);
		Assert.That(actual: isVerified, expression: Is.True);
	}

	[Test]
	public async Task RegistrationCodeVerification_WithIncorrectVerificationCode_ShouldReturnFalse()
	{
		RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			RegistrationCode = "testttt"
		};
		bool isVerified = await registrationCodeVerificationService.Verify(credentials: userCredentials);
		Assert.That(actual: isVerified, expression: Is.False);
	}

	[Test]
	public async Task RegistrationCodeVerification_WithEmptyVerificationCode_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = String.Empty
			};
			_ = await registrationCodeVerificationService.Verify(credentials: userCredentials);
		});
	}

	[Test]
	public async Task RegistrationCodeVerification_WithVerificationCodeIsNull_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = null
			};
			_ = await registrationCodeVerificationService.Verify(credentials: userCredentials);
		});
	}

	[Test]
	public async Task RegistrationCodeVerification_WithShortVerificationCode_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = "123"
			};
			_ = await registrationCodeVerificationService.Verify(credentials: userCredentials);
		});
	}

	[Test]
	public async Task RegistrationCodeVerification_WithLongVerificationCode_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = "testtest"
			};
			_ = await registrationCodeVerificationService.Verify(credentials: userCredentials);
		});
	}
}