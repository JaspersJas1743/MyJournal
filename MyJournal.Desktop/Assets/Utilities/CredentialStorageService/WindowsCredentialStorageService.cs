using System.Net;
using System.Runtime.Versioning;
using AdysTech.CredentialManager;
using Microsoft.Extensions.DependencyInjection;

namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

[SupportedOSPlatform(platformName: "Windows")]
public class WindowsCredentialStorageService : ICredentialStorageService
{
	private const string CredentialKey = "MyJournalUser";

	public UserCredential Get()
	{
		NetworkCredential? credentials = CredentialManager.GetCredentials(target: CredentialKey);
		return credentials is null
			? UserCredential.Empty
			: new UserCredential(Login: credentials.UserName, AccessToken: credentials.Password);
	}

	public void Set(UserCredential credential)
	{
		NetworkCredential credentials = new NetworkCredential(userName: credential.Login, password: credential.AccessToken);
		CredentialManager.SaveCredentials(target: CredentialKey, credential: credentials);
	}

	public void Remove()
		=> CredentialManager.RemoveCredentials(target: CredentialKey);
}

public static class WindowsCredentialStorageServiceExtensions
{
#pragma warning disable CA1416
	public static IServiceCollection AddWindowsCredentialStorageService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<ICredentialStorageService, WindowsCredentialStorageService>();

	public static IServiceCollection AddKeyedWindowsCredentialStorageService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<ICredentialStorageService, WindowsCredentialStorageService>(serviceKey: key);
#pragma warning restore CA1416
}