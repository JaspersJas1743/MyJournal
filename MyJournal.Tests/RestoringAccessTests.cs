using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class RestoringAccessTests
{
	private ServiceProvider _serviceProvider = null!;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddGoogleAuthenticator();
		serviceCollection.AddKeyedTransient<IRestoringAccessService<User>, RestoringAccessThroughEmailService>(serviceKey: nameof(RestoringAccessThroughEmailService));
		serviceCollection.AddKeyedTransient<IRestoringAccessService<User>, RestoringAccessThroughPhoneService>(serviceKey: nameof(RestoringAccessThroughPhoneService));
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}

	[Test]
	public async Task UserRestoringAccessThroughEmailService_WithCorrectCredentials_ShouldReturnTrue()
	{
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughEmailService))!;
		EmailCredentials emailCredentials = new EmailCredentials()
		{
			Email = "lesha.smirnov2019@mail.ru"
		};
		_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		await userRegistrationService.VerifyAuthenticationCode(code: "499980");
		await userRegistrationService.ResetPassword(newPassword: "bratbratubrat");
		Assert.Pass();
	}

	[Test]
	public void UserRestoringAccessThroughEmailService_WithCorrectCredentialsAndUsedNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughEmailService))!;
			EmailCredentials emailCredentials = new EmailCredentials()
			{
				Email = "lesha.smirnov2019@mail.ru"
			};
			_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
			await userRegistrationService.VerifyAuthenticationCode(code: "499980");
			await userRegistrationService.ResetPassword(newPassword: "JaspersJas1743");
		});
	}

	[Test]
	public void UserRestoringAccessThroughEmailService_WithCorrectCredentialsAndIncorrectAuthenticationCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<Exception>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughEmailService))!;
			EmailCredentials emailCredentials = new EmailCredentials()
			{
				Email = "lesha.smirnov2019@mail.ru"
			};
			_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
			bool isVerified = await userRegistrationService.VerifyAuthenticationCode(code: "123456");
			if (!isVerified)
				throw new Exception();
			await userRegistrationService.ResetPassword(newPassword: "bratbratubrat");
		});
	}

	[Test]
	public async Task UserRestoringAccessThroughEmailService_WithIncorrectCredentials_ShouldReturnFalse()
	{
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughEmailService))!;
		PhoneCredentials emailCredentials = new PhoneCredentials()
		{
			Phone = "ivanivanovich@mail.ru"
		};
		bool isVerified = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		Assert.That(actual: isVerified, expression: Is.False);
	}

	[Test]
	public async Task UserRestoringAccessThroughPhoneService_WithCorrectCredentials_ShouldReturnTrue()
	{
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughPhoneService))!;
		PhoneCredentials emailCredentials = new PhoneCredentials()
		{
			Phone = "+7(910)952-0836"
		};
		_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		await userRegistrationService.VerifyAuthenticationCode(code: "499980");
		await userRegistrationService.ResetPassword(newPassword: "bratbratubrat2");
		Assert.Pass();
	}

	[Test]
	public void UserRestoringAccessThroughPhoneService_WithCorrectCredentialsAndUsedNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughPhoneService))!;
			PhoneCredentials emailCredentials = new PhoneCredentials()
			{
				Phone = "+7(910)952-0836"
			};
			_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
			await userRegistrationService.VerifyAuthenticationCode(code: "499980");
			await userRegistrationService.ResetPassword(newPassword: "JaspersJas1743");
		});
	}

	[Test]
	public void UserRestoringAccessThroughPhoneService_WithCorrectCredentialsAndIncorrectAuthenticationCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<Exception>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughPhoneService))!;
			PhoneCredentials emailCredentials = new PhoneCredentials()
			{
				Phone = "+7(910)952-0836"
			};
			_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
			bool isVerified = await userRegistrationService.VerifyAuthenticationCode(code: "123456");
			if (!isVerified)
				throw new Exception();
			await userRegistrationService.ResetPassword(newPassword: "bratbratubrat");
		});
	}

	[Test]
	public async Task UserRestoringAccessThroughPhoneService_WithIncorrectCredentials_ShouldReturnFalse()
	{
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>(nameof(RestoringAccessThroughPhoneService))!;
		PhoneCredentials emailCredentials = new PhoneCredentials()
		{
			Phone = "+7(910)952-0835"
		};
		bool isVerified = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		Assert.That(actual: isVerified, expression: Is.False);
	}
}