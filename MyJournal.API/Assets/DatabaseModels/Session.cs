using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Session
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Ip { get; set; } = null!;

    public int MyJournalClientId { get; set; }

    public int SessionActivityStatusId { get; set; }

    public virtual MyJournalClient MyJournalClient { get; set; } = null!;

    public virtual SessionActivityStatus SessionActivityStatus { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
