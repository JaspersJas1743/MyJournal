using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using MyJournal.Core.Utilities;

namespace MyJournal.Tests;

public class RestoringAccessTests
{
	private ServiceProvider _serviceProvider;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddKeyedTransient<IRestoringAccessService<User>, RestoringAccessThroughEmailService>(serviceKey: "RestoringAccessThroughEmailService");
		serviceCollection.AddKeyedTransient<IRestoringAccessService<User>, RestoringAccessThroughPhoneService>(serviceKey: "RestoringAccessThroughPhoneService");
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
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughEmailService");
		EmailCredentials emailCredentials = new EmailCredentials()
		{
			Email = "lesha.smirnov@mail.ru"
		};
		_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		await userRegistrationService.VerifyAuthenticationCode(code: "345015");
		await userRegistrationService.ResetPassword(newPassword: "bratbratubrat");
		Assert.Pass();
	}

	[Test]
	public async Task UserRestoringAccessThroughEmailService_WithCorrectCredentialsAndUsedNewPassword_ShouldThrowError()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughEmailService");
			EmailCredentials emailCredentials = new EmailCredentials()
			{
				Email = "lesha.smirnov@mail.ru"
			};
			_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
			await userRegistrationService.VerifyAuthenticationCode(code: "557944");
			await userRegistrationService.ResetPassword(newPassword: "JaspersJas1743");
		});
	}

	[Test]
	public async Task UserRestoringAccessThroughEmailService_WithCorrectCredentialsAndIncorrectAuthenticationCode_ShouldThrowError()
	{
		Assert.ThrowsAsync<Exception>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughEmailService");
			EmailCredentials emailCredentials = new EmailCredentials()
			{
				Email = "lesha.smirnov@mail.ru"
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
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughEmailService");
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
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughPhoneService");
		PhoneCredentials emailCredentials = new PhoneCredentials()
		{
			Phone = "+7(910)952-0836"
		};
		_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		await userRegistrationService.VerifyAuthenticationCode(code: "557944");
		await userRegistrationService.ResetPassword(newPassword: "bratbratubrat2");
		Assert.Pass();
	}

	[Test]
	public async Task UserRestoringAccessThroughPhoneService_WithCorrectCredentialsAndUsedNewPassword_ShouldThrowError()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughPhoneService");
			PhoneCredentials emailCredentials = new PhoneCredentials()
			{
				Phone = "+7(910)952-0836"
			};
			_ = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
			await userRegistrationService.VerifyAuthenticationCode(code: "557944");
			await userRegistrationService.ResetPassword(newPassword: "JaspersJas1743");
		});
	}

	[Test]
	public async Task UserRestoringAccessThroughPhoneService_WithCorrectCredentialsAndIncorrectAuthenticationCode_ShouldThrowError()
	{
		Assert.ThrowsAsync<Exception>(code: async () =>
		{
			IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughPhoneService");
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
		IRestoringAccessService<User> userRegistrationService = _serviceProvider.GetKeyedService<IRestoringAccessService<User>>("RestoringAccessThroughPhoneService");
		PhoneCredentials emailCredentials = new PhoneCredentials()
		{
			Phone = "+7(910)952-0835"
		};
		bool isVerified = await userRegistrationService.VerifyCredential(credentials: emailCredentials);
		Assert.That(actual: isVerified, expression: Is.False);
	}
}