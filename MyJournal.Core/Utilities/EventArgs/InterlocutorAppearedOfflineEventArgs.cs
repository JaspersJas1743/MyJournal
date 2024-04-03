namespace MyJournal.Core.Utilities.EventArgs;

public sealed class InterlocutorAppearedOfflineEventArgs(DateTime? onlineAt, int interlocutorId) : ChangeOnlineStatusEventArgs(interlocutorId, onlineAt);
