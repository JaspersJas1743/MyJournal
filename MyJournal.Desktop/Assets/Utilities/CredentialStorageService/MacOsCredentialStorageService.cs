using System;
using System.Runtime.Versioning;
using GnomeStack.Secrets.Darwin;

namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

[Untested]
[SupportedOSPlatform(platformName: "MacOS")]
public class MacOsCredentialStorageService : ICredentialStorageService
{
	private const string MyJournalService = nameof(MyJournalService);
	private const string Login = nameof(Login);
	private const string Password = nameof(Password);

	private static readonly DarwinOsSecretVault SecretVault = new DarwinOsSecretVault();

	public UserCredential Get()
	{
		string login = SecretVault.GetSecret(service: MyJournalService, account: Login) ?? String.Empty;
		string password = SecretVault.GetSecret(service: MyJournalService, account: Password) ?? String.Empty;
		return new UserCredential(Login: login, Password: password);
	}

	public void Set(UserCredential credential)
	{
		SecretVault.SetSecret(service: MyJournalService, account: Login, secret: credential.Login);
		SecretVault.SetSecret(service: MyJournalService, account: Password, secret: credential.Password);
	}

	public void Remove()
	{
		SecretVault.DeleteSecret(service: MyJournalService, account: Login);
		SecretVault.DeleteSecret(service: MyJournalService, account: Password);
	}
}