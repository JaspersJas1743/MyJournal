namespace MyJournal.Core.Utilities.EventArgs;

public sealed class InterlocutorUpdatedPhotoEventArgs(string link, int interlocutorId) : System.EventArgs
{
	public int InterlocutorId { get; } = interlocutorId;
	public string Link { get; } = link;
}