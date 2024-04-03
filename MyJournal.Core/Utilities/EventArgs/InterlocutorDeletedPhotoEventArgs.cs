namespace MyJournal.Core.Utilities.EventArgs;

public sealed class InterlocutorDeletedPhotoEventArgs(int interlocutorId) : System.EventArgs
{
	public int InterlocutorId { get; } = interlocutorId;
}