using System;

namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

public sealed record UserCredential(string Login, string Password)
{
	public static readonly UserCredential Empty = new UserCredential(Login: String.Empty, Password: String.Empty);
}