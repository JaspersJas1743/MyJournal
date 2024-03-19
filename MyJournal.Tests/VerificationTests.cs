using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Tests;

public class VerificationTests
{
	private ServiceProvider _serviceProvider = null!;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddTransient<IVerificationService<Credentials<User>>, RegistrationCodeVerificationService>();
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}


	[Test]
	public async Task RegistrationCodeVerification_WithCorrectVerificationCode_ShouldReturnTrue()
	{
		IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
		UserCredentials userCredentials = new UserCredentials()
		{
			RegistrationCode = "testtes"
		};
		bool isVerified = await registrationCodeVerificationService.Verify(toVerifying: userCredentials);
		Assert.That(actual: isVerified, expression: Is.True);
	}

	[Test]
	public async Task RegistrationCodeVerification_WithIncorrectVerificationCode_ShouldReturnFalse()
	{
		IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
		UserCredentials userCredentials = new UserCredentials()
		{
			RegistrationCode = "testttt"
		};
		bool isVerified = await registrationCodeVerificationService.Verify(toVerifying: userCredentials);
		Assert.That(actual: isVerified, expression: Is.False);
	}

	[Test]
	public void RegistrationCodeVerification_WithEmptyVerificationCode_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = String.Empty
			};
			_ = await registrationCodeVerificationService.Verify(toVerifying: userCredentials);
		});
	}

	[Test]
	public void RegistrationCodeVerification_WithVerificationCodeIsNull_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = null
			};
			_ = await registrationCodeVerificationService.Verify(toVerifying: userCredentials);
		});
	}

	[Test]
	public void RegistrationCodeVerification_WithShortVerificationCode_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = "123"
			};
			_ = await registrationCodeVerificationService.Verify(toVerifying: userCredentials);
		});
	}

	[Test]
	public void RegistrationCodeVerification_WithLongVerificationCode_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IVerificationService<Credentials<User>> registrationCodeVerificationService = _serviceProvider.GetService<IVerificationService<Credentials<User>>>()!;
			UserCredentials userCredentials = new UserCredentials()
			{
				RegistrationCode = "testtest"
			};
			_ = await registrationCodeVerificationService.Verify(toVerifying: userCredentials);
		});
	}
}