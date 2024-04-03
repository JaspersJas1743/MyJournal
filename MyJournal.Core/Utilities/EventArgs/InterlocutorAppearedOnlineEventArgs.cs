namespace MyJournal.Core.Utilities.EventArgs;

public sealed class InterlocutorAppearedOnlineEventArgs(DateTime? onlineAt, int interlocutorId) : ChangeOnlineStatusEventArgs(interlocutorId, onlineAt);