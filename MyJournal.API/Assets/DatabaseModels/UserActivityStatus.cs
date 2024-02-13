using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum UserActivityStatuses
{
    Online,
    Offline
}

public partial class UserActivityStatus
{
    public int Id { get; set; }

    public UserActivityStatuses ActivityStatus { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
