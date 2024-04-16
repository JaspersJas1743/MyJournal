using System.Net;
using System.Runtime.Versioning;
using AdysTech.CredentialManager;

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
			: new UserCredential(Login: credentials.UserName, Password: credentials.Password);
	}

	public void Set(UserCredential credential)
	{
		NetworkCredential credentials = new NetworkCredential(userName: credential.Login, password: credential.Password);
		CredentialManager.SaveCredentials(target: CredentialKey, credential: credentials);
	}

	public void Remove()
		=> CredentialManager.RemoveCredentials(target: CredentialKey);
}