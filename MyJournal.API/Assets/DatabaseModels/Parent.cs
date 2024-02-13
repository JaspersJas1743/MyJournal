using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Parent
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ChildrenId { get; set; }

    public virtual Student Children { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
