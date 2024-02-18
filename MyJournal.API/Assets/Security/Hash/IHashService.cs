namespace MyJournal.API.Assets.Security.Hash;

public interface IHashService
{
	string Generate(string toHash);
	bool Verify(string? text, string? hashedText);
}