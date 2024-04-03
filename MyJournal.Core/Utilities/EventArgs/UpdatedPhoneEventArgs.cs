namespace MyJournal.Core.Utilities.EventArgs;

public sealed class UpdatedPhoneEventArgs(string? phone) : System.EventArgs
{
	public string? Phone { get; } = phone;
}