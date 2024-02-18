namespace MyJournal.API.Assets.Security.Hash;

public sealed class BCryptHashService : IHashService
{
	public string Generate(string toHash)
		=> BCrypt.Net.BCrypt.HashPassword(inputKey: toHash);

	public bool Verify(string? text, string? hash)
		=> BCrypt.Net.BCrypt.Verify(text: text, hash: hash);
}