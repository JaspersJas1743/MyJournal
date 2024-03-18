using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class UserTests
{
	#region SetUp
	private ServiceProvider _serviceProvider;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddTransient<IGoogleAuthenticatorService, GoogleAuthenticatorService>();
		serviceCollection.AddTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>();
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}
	#endregion

	#region SessionsTest
	[Test]
	public async Task UserSignOutThisSession_WithOneTry_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		_ = await user.Security.SignOutThisSession();
		Assert.Pass();
	}

	[Test]
	public async Task UserSignOutThisSession_WithTwoTry_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			_ = await user.Security.SignOutThisSession();
			_ = await user.Security.SignOutThisSession();
		});
	}

	[Test]
	public async Task UserSignOutAllSession_WithTryingGetDataFromAnySession_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			User user2 = await service2.SignIn(credentials: credentials);
			await user.Security.SignOutAllSessions();
			await user2.Photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
		});
	}

	[Test]
	public async Task UserSignOutOthersSession_WithTryingGetDataFromOtherSession_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			User user2 = await service2.SignIn(credentials: credentials);
			await user.Security.SignOutOthersSessions();
			await user2.Photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
		});
	}

	[Test]
	public async Task UserSignOutOthersSession_WithTryingGetDataFromThisSession_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Security.SignOutOthersSessions();
		await user.Photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
	}
	#endregion

	#region ChangeEmail
	[Test]
	public async Task UserChangeEmail_WithCorrectEmail_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		string newEmail = "test@mail.ru";
		await user.Security.Email!.Change(confirmationCode: "785177", newEmail: newEmail);
		Assert.That(actual: user.Security.Email.Address, expression: Is.EqualTo(expected: newEmail));
	}

	[Test]
	public async Task UserChangeEmail_WithUsedEmail_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email!.Change(confirmationCode: "785177", newEmail: "test@mail.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithIncorrectEmailName_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email!.Change(confirmationCode: "785177", newEmail: "@mail.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithIncorrectEmailDomain_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email!.Change(confirmationCode: "785177", newEmail: "test@.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithoutEmailDomain_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email!.Change(confirmationCode: "785177", newEmail: "test@");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithoutAt_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email!.Change(confirmationCode: "785177", newEmail: "testmail.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithIncorrectCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<ArgumentException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email!.Change(confirmationCode: "000000", newEmail: "testmail.ru");
		});
	}
	#endregion

	#region ChangePhone
	[Test]
	public async Task UserChangePhone_WithCorrectPhone_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Security.Phone!.Change(confirmationCode: "006366", newPhone: "+7(777)777-7777");
	}

	[Test]
	public async Task UserChangePhone_WithUsedPhone_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone!.Change(confirmationCode: "006366", newPhone: "+7(777)777-7777");
		});
	}

	[Test]
	public async Task UserChangePhone_WithIncorrectPhone_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone!.Change(confirmationCode: "006366", newPhone: "123453");
		});
	}

	[Test]
	public async Task UserChangePhone_WithIncorrectFormat_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone!.Change(confirmationCode: "006366", newPhone: "9999999999");
		});
	}

	[Test]
	public async Task UserChangePhone_WithoutPlus_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone!.Change(confirmationCode: "006366", newPhone: "7(999)999-9999");
		});
	}

	[Test]
	public async Task UserChangePhone_WithIncorrectFormat2_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone!.Change(confirmationCode: "006366", newPhone: "79999999999");
		});
	}

	[Test]
	public async Task UserChangePhone_WithIncorrectCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<ArgumentException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone!.Change(confirmationCode: "000000", newPhone: "+7(999)999-9999");
		});
	}
	#endregion

	#region ChangePassword
	[Test]
	public async Task UserChangePassword_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Security.Password!.Change(confirmationCode: "269088", currentPassword: "JaspersJas1743", newPassword: "Jaspers");
	}

	[Test]
	public async Task UserChangePassword_WithIncorrectCurrentPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password!.Change(confirmationCode: "269088", currentPassword: "Jaspers", newPassword: "JaspersJas1743");
		});
	}

	[Test]
	public async Task UserChangePassword_WithEqualsCurrentAndNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password!.Change(confirmationCode: "269088", currentPassword: "Jaspers", newPassword: "Jaspers");
		});
	}

	[Test]
	public async Task UserChangePassword_WithIncorrectFormatNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password!.Change(confirmationCode: "269088", currentPassword: "Jaspers", newPassword: "J");
		});
	}

	[Test]
	public async Task UserChangePassword_WithShortNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password!.Change(confirmationCode: "269088", currentPassword: "Jaspers", newPassword: "Js");
		});
	}

	[Test]
	public async Task UserChangePassword_WithIncorrectCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<ArgumentException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "Jaspers",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password!.Change(confirmationCode: "000000", currentPassword: "JaspersJas1743", newPassword: "Js");
		});
	}
	#endregion

	#region Chats
	[Test]
	public async Task UserGetChats_WithNullFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Chats.SetFilter(filter: null);
	}

	[Test]
	public async Task UserGetChats_WithEmptyFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Chats.SetFilter(filter: String.Empty);
	}

	[Test]
	public async Task UserGetChats_WithFilterIsWhiteSpace_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Chats.SetFilter(filter: "    ");
	}
	#endregion

	#region Interlocutors
	[Test]
	public async Task UserGetInterlocutors_WithNullFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Interlocutors.SetFilter(filter: null);
		Assert.That(actual: user.Interlocutors[0].Surname, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: user.Interlocutors[0].Name, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: user.Interlocutors[0].Patronymic, expression: Is.EqualTo(expected: "test"));	}

	[Test]
	public async Task UserGetInterlocutors_WithEmptyFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Interlocutors.SetFilter(filter: String.Empty);
		Assert.That(actual: user.Interlocutors[0].Surname, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: user.Interlocutors[0].Name, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: user.Interlocutors[0].Patronymic, expression: Is.EqualTo(expected: "test"));	}

	[Test]
	public async Task UserGetInterlocutors_WithFilterIsWhiteSpace_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>();
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Interlocutors.SetFilter(filter: "   ");
		Assert.That(actual: user.Interlocutors[0].Surname, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: user.Interlocutors[0].Name, expression: Is.EqualTo(expected: "test"));
		Assert.That(actual: user.Interlocutors[0].Patronymic, expression: Is.EqualTo(expected: "test"));
	}
	#endregion
}