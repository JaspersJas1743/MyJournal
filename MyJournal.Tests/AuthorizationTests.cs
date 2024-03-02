using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities;

namespace MyJournal.Tests;

public class AuthorizationTests
{
	private ServiceProvider _serviceProvider;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddKeyedTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>(serviceKey: "AuthorizationWithCredentialsService");
		serviceCollection.AddKeyedTransient<IAuthorizationService<User>, AuthorizationWithTokenService>(serviceKey: "AuthorizationWithTokenService");
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithCorrectData_ShouldReturnUser()
	{
		IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		_ = await service.SignIn(credentials: credentials);
		Assert.Pass();
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithIncorrectLoginAndCorrectPassword_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jasper",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithIncorrectPasswordAndCorrectLogin_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithEmptyLogin_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: String.Empty,
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithLoginIsNull_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: null,
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithShortLoginLength_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jas",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithEmptyPassword_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: String.Empty,
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithPasswordIsNull_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: null,
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithCredentials_WithShortPasswordLength_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithCredentialsService");
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "Jas",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithToken_WithCorrectToken_ShouldReturnUser()
	{
		IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithTokenService");
		UserTokenCredentials credentials = new UserTokenCredentials(
			token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJteWpvdXJuYWw6aWRlbnRpZmllciI6IjUiLCJteWpvdXJuYWw6cm9sZSI6IlN0dWRlbnQiLCJteWpvdXJuYWw6c2Vzc2lvbiI6IjEwOTQiLCJpc3MiOiJKYXNwZXJzSmFzMTc0MyIsImF1ZCI6Ik15Sm91cm5hbFVzZXIifQ.Z4mASF3KO4ozfqCqheiC8nrh0ZE1dOAeIkgA8UZk0bI"
		);
		_ = await service.SignIn(credentials: credentials);
		Assert.Pass();
	}

	[Test]
	public async Task AuthorizationWithToken_WithIncorrectToken_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithTokenService");
			UserTokenCredentials credentials = new UserTokenCredentials(
				token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..O8yQXKcI9HaLsg8KM39ByE8PS9OqHtij4hQfdrsiCvQ"
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithToken_WithEmptyToken_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithTokenService");
			UserTokenCredentials credentials = new UserTokenCredentials(
				token: String.Empty
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public async Task AuthorizationWithToken_WithTokenIsNulls_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: "AuthorizationWithTokenService");
			UserTokenCredentials credentials = new UserTokenCredentials(
				token: null
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}
}