using System;
using GnomeStack.Secrets;
using GnomeStack.Standard;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

public class LinuxCredentialStorageService : ICredentialStorageService
{
    private const string MyJournalService = nameof(MyJournalService);
    private const string Login = nameof(Login);
    private const string Password = nameof(Password);

    public UserCredential Get()
    {
        string login = OsSecretVault.GetSecret(service: MyJournalService, account: Login) ?? String.Empty;
        string password = OsSecretVault.GetSecret(service: MyJournalService, account: Password) ?? String.Empty;
        return new UserCredential(Login: login, Password: password);
    }

    public void Set(UserCredential credential)
    {
        OsSecretVault.SetSecret(service: MyJournalService, account: Login, secret: credential.Login);
        OsSecretVault.SetSecret(service: MyJournalService, account: Password, secret: credential.Password);
    }

    public void Remove()
    {
        OsSecretVault.DeleteSecret(service: MyJournalService, account: Login);
        OsSecretVault.DeleteSecret(service: MyJournalService, account: Password);
    }
}