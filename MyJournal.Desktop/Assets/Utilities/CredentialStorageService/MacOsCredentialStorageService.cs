using System;
using System.Runtime.Versioning;
using GnomeStack.Secrets.Darwin;
using Microsoft.Extensions.DependencyInjection;

namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

[Untested(comment: "Хранилище учетных данных для macOS не было протестировано.")]
[SupportedOSPlatform(platformName: "MacOS")]
public class MacOsCredentialStorageService : ICredentialStorageService
{
	private const string MyJournalService = nameof(MyJournalService);
	private const string Login = nameof(Login);
	private const string AccessToken = nameof(AccessToken);

	private static readonly DarwinOsSecretVault SecretVault = new DarwinOsSecretVault();

	public UserCredential Get()
	{
		string login = SecretVault.GetSecret(service: MyJournalService, account: Login) ?? String.Empty;
		string accessToken = SecretVault.GetSecret(service: MyJournalService, account: AccessToken) ?? String.Empty;
		return new UserCredential(Login: login, AccessToken: accessToken);
	}

	public void Set(UserCredential credential)
	{
		SecretVault.SetSecret(service: MyJournalService, account: Login, secret: credential.Login);
		SecretVault.SetSecret(service: MyJournalService, account: AccessToken, secret: credential.AccessToken);
	}

	public void Remove()
	{
		SecretVault.DeleteSecret(service: MyJournalService, account: Login);
		SecretVault.DeleteSecret(service: MyJournalService, account: AccessToken);
	}
}

public static class MacOsCredentialStorageServiceExtensions
{
#pragma warning disable CA1416
	public static IServiceCollection AddMacOsCredentialStorageService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<ICredentialStorageService, MacOsCredentialStorageService>();

	public static IServiceCollection AddKeyedMacOsCredentialStorageService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<ICredentialStorageService, MacOsCredentialStorageService>(serviceKey: key);
#pragma warning restore CA1416
}