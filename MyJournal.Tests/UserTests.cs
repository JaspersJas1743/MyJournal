using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities;

namespace MyJournal.Tests;

public class UserTests
{
	private ServiceProvider _serviceProvider;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>();
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}


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
		_ = await user.SignOut();
		Assert.Pass();
	}

	[Test]
	public async Task UserSignOutThisSession_WithTwoTry_ShouldThrowError()
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
			_ = await user.SignOut();
			_ = await user.SignOut();
		});
	}

	[Test]
	public async Task UserSignOutAllSession_WithTryingGetDataFromAnySession_ShouldThrowError()
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
			await user.SignOutAll();
			await user2.DownloadProfilePhoto(folderToSave: @"C:\Users\JaspersJas1743\Downloads");
		});
	}

	[Test]
	public async Task UserSignOutOthersSession_WithTryingGetDataFromOtherSession_ShouldThrowError()
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
			await user.SignOutAllExceptThis();
			await user2.DownloadProfilePhoto(folderToSave: @"C:\Users\JaspersJas1743\Downloads");
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
		await user.SignOutAllExceptThis();
		await user.DownloadProfilePhoto(folderToSave: @"C:\Users\JaspersJas1743\Downloads");
	}

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
		await user.ChangeEmail(email: "test@mail.ru");
	}

	[Test]
	public async Task UserChangeEmail_WithUsedEmail_ShouldThrowError()
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
			await user.ChangeEmail(email: "test@mail.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithIncorrectEmailName_ShouldThrowError()
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
			await user.ChangeEmail(email: "@mail.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithIncorrectEmailDomain_ShouldThrowError()
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
			await user.ChangeEmail(email: "test@.ru");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithoutEmailDomain_ShouldThrowError()
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
			await user.ChangeEmail(email: "test@");
		});
	}

	[Test]
	public async Task UserChangeEmail_WithoutAt_ShouldThrowError()
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
			await user.ChangeEmail(email: "testmail.ru");
		});
	}
}