using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum UserRoles
{
    Student,
    Teacher,
    Administrator,
    Parent
}

public partial class UserRole
{
    public int Id { get; set; }

    public UserRoles Role { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
