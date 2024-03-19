using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class AuthorizationTests
{
	private ServiceProvider _serviceProvider = null!;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddGoogleAuthenticator();
		serviceCollection.AddKeyedTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>(serviceKey: nameof(AuthorizationWithCredentialsService));
		serviceCollection.AddKeyedTransient<IAuthorizationService<User>, AuthorizationWithTokenService>(serviceKey: nameof(AuthorizationWithTokenService));
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
		IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		_ = await service.SignIn(credentials: credentials);
		Assert.Pass();
	}

	[Test]
	public void AuthorizationWithCredentials_WithIncorrectLoginAndCorrectPassword_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jasper",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithIncorrectPasswordAndCorrectLogin_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithEmptyLogin_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: String.Empty,
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithLoginIsNull_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
#pragma warning disable CS8625
				login: null,
#pragma warning restore CS8625
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithShortLoginLength_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jas",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithEmptyPassword_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: String.Empty,
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithPasswordIsNull_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
#pragma warning disable CS8625
				password: null,
#pragma warning restore CS8625
				client: UserAuthorizationCredentials.Clients.Windows
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithCredentials_WithShortPasswordLength_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithCredentialsService))!;
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
		IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithTokenService))!;
		UserTokenCredentials credentials = new UserTokenCredentials(
			token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJteWpvdXJuYWw6aWRlbnRpZmllciI6IjUiLCJteWpvdXJuYWw6cm9sZSI6IlN0dWRlbnQiLCJteWpvdXJuYWw6c2Vzc2lvbiI6IjI0NjQiLCJpc3MiOiJKYXNwZXJzSmFzMTc0MyIsImF1ZCI6Ik15Sm91cm5hbFVzZXIifQ.rkWcftJyRTgZf7vGBO7EHMqnH_O7uYEusgw38A2F9TE"
		);
		_ = await service.SignIn(credentials: credentials);
		Assert.Pass();
	}

	[Test]
	public void AuthorizationWithToken_WithIncorrectToken_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithTokenService))!;
			UserTokenCredentials credentials = new UserTokenCredentials(
				token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..O8yQXKcI9HaLsg8KM39ByE8PS9OqHtij4hQfdrsiCvQ"
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithToken_WithEmptyToken_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithTokenService))!;
			UserTokenCredentials credentials = new UserTokenCredentials(
				token: String.Empty
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}

	[Test]
	public void AuthorizationWithToken_WithTokenIsNulls_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetKeyedService<IAuthorizationService<User>>(serviceKey: nameof(AuthorizationWithTokenService))!;
			UserTokenCredentials credentials = new UserTokenCredentials(
#pragma warning disable CS8625
				token: null
#pragma warning restore CS8625
			);
			_ = await service.SignIn(credentials: credentials);
		});
	}
}