namespace MyJournal.Core.Utilities.EventArgs;

public sealed class UpdatedProfilePhotoEventArgs(string? link) : System.EventArgs
{
	public string? Link { get; } = link;
}