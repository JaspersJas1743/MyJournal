using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class RegistrationTests
{
	private ServiceProvider _serviceProvider = null!;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddGoogleAuthenticator();
		serviceCollection.AddTransient<IRegistrationService<User>, UserRegistrationService>();
		serviceCollection.AddTransient<IVerificationService<Credentials<User>>, RegistrationCodeVerificationService>();
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}

	[Test]
	public async Task UserRegistration_WithCorrectData_ShouldReturnTrue()
	{
		IRegistrationService<User> userRegistrationService = _serviceProvider.GetService<IRegistrationService<User>>()!;
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
		IRegistrationService<User> userRegistrationService = _serviceProvider.GetService<IRegistrationService<User>>()!;
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
		IRegistrationService<User> userRegistrationService = _serviceProvider.GetService<IRegistrationService<User>>()!;
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
		IRegistrationService<User> userRegistrationService = _serviceProvider.GetService<IRegistrationService<User>>()!;
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Ivan",
			Password = "IvanIvanovich",
			RegistrationCode = "1234567"
		};
		IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials, verifier: registrationCodeVerificationService);
		Assert.That(actual: isRegistered, expression: Is.True);
	}

	[Test]
	public async Task UserRegistration_WithVerificationAndIncorrectRegistrationCode_ShouldReturnFalse()
	{
		IRegistrationService<User> userRegistrationService = _serviceProvider.GetService<IRegistrationService<User>>()!;
		UserCredentials userCredentials = new UserCredentials()
		{
			Login = "Jaspers",
			Password = "IvanIvanovich",
			RegistrationCode = "1111111"
		};
		IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
		bool isRegistered = await userRegistrationService.Register(credentials: userCredentials, verifier: registrationCodeVerificationService);
		Assert.That(actual: isRegistered, expression: Is.False);
	}
}