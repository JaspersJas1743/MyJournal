using System;

namespace MyJournal.Desktop.Assets.Utilities.CredentialStorageService;

public sealed record UserCredential(string Login, string AccessToken)
{
	public static readonly UserCredential Empty = new UserCredential(Login: String.Empty, AccessToken: String.Empty);
}