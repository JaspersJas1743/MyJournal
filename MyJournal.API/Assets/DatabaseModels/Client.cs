using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum Clients
{
    Windows,
    Linux,
    Chrome,
    Opera,
    Yandex,
    Other
}

public partial class Client
{
    public int Id { get; set; }

    public Clients ClientName { get; set; }

    public virtual ICollection<MyJournalClient> MyJournalClients { get; set; } = new List<MyJournalClient>();
}
