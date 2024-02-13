using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum SessionActivityStatuses
{
    Disable,
    Enable
}

public partial class SessionActivityStatus
{
    public int Id { get; set; }

    public SessionActivityStatuses ActivityStatus { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
