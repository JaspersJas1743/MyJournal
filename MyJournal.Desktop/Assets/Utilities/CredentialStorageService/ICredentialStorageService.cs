namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

public interface ICredentialStorageService
{
	UserCredential Get();
	void Set(UserCredential credential);
	void Remove();
}