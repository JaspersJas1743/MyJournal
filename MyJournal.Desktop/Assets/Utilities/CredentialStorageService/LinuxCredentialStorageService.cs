using System;
using System.Runtime.Versioning;
using GnomeStack.Standard;
using Microsoft.Extensions.DependencyInjection;

namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

[Untested]
[SupportedOSPlatform(platformName: "Linux")]
public class LinuxCredentialStorageService : ICredentialStorageService
{
    private const string MyJournalService = nameof(MyJournalService);
    private const string Login = nameof(Login);
    private const string AccessToken = nameof(AccessToken);

    public UserCredential Get()
    {
        string login = OsSecretVault.GetSecret(service: MyJournalService, account: Login) ?? String.Empty;
        string accessToken = OsSecretVault.GetSecret(service: MyJournalService, account: AccessToken) ?? String.Empty;
        return new UserCredential(Login: login, AccessToken: accessToken);
    }

    public void Set(UserCredential credential)
    {
        OsSecretVault.SetSecret(service: MyJournalService, account: Login, secret: credential.Login);
        OsSecretVault.SetSecret(service: MyJournalService, account: AccessToken, secret: credential.AccessToken);
    }

    public void Remove()
    {
        OsSecretVault.DeleteSecret(service: MyJournalService, account: Login);
        OsSecretVault.DeleteSecret(service: MyJournalService, account: AccessToken);
    }
}

public static class LinuxCredentialStorageServiceExtensions
{
#pragma warning disable CA1416
    public static IServiceCollection AddLinuxCredentialStorageService(this IServiceCollection serviceCollection)
        => serviceCollection.AddTransient<ICredentialStorageService, LinuxCredentialStorageService>();

    public static IServiceCollection AddKeyedLinuxCredentialStorageService(this IServiceCollection serviceCollection, string key)
        => serviceCollection.AddKeyedTransient<ICredentialStorageService, LinuxCredentialStorageService>(serviceKey: key);
#pragma warning restore CA1416
}