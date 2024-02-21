using MyJournal.Core.Registration;

namespace MyJournal.Tests;

public class RegistrationTests
{
	[Test]
	public async Task UserRegistration_WithCorrectData_ShouldReturnTrue()
	{
		UserRegistrationService userRegistrationService = new UserRegistrationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Ivan",
			Password = "IvanIvanovich",
			RegistrationCode = "1234567"
		};
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials);
		Assert.That(actual: isRegistered, expression: Is.True);
	}

	[Test]
	public async Task UserRegistration_WithIncorrectRegistrationCode_ShouldReturnFalse()
	{
		UserRegistrationService userRegistrationService = new UserRegistrationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Jaspers",
			Password = "IvanIvanovich",
			RegistrationCode = "11111111"
		};
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials);
		Assert.That(actual: isRegistered, expression: Is.False);
	}

	[Test]
	public async Task UserRegistration_WithUsedLogin_ShouldReturnFalse()
	{
		UserRegistrationService userRegistrationService = new UserRegistrationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Jaspers",
			Password = "IvanIvanovich",
			RegistrationCode = "1234567"
		};
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials);
		Assert.That(actual: isRegistered, expression: Is.False);
	}

	[Test]
	public async Task UserRegistration_WithVerificationAndCorrectData_ShouldReturnTrue()
	{
		UserRegistrationService userRegistrationService = new UserRegistrationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Ivan",
			Password = "IvanIvanovich",
			RegistrationCode = "1234567"
		};
		RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials, verifier: registrationCodeVerificationService);
		Assert.That(actual: isRegistered, expression: Is.True);
	}

	[Test]
	public async Task UserRegistration_WithVerificationAndIncorrectRegistrationCode_ShouldReturnFalse()
	{
		UserRegistrationService userRegistrationService = new UserRegistrationService();
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Jaspers",
			Password = "IvanIvanovich",
			RegistrationCode = "1111111"
		};
		RegistrationCodeVerificationService registrationCodeVerificationService = new RegistrationCodeVerificationService();
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials, verifier: registrationCodeVerificationService);
		Assert.That(actual: isRegistered, expression: Is.False);
	}
}