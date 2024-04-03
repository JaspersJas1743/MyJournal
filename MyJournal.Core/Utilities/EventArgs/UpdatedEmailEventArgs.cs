namespace MyJournal.Core.Utilities.EventArgs;

public sealed class UpdatedEmailEventArgs(string? email) : System.EventArgs
{
	public string? Email { get; } = email;
}