using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class MyJournalClient
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public string LinkToLogo { get; set; } = null!;

    public string ClientName { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
