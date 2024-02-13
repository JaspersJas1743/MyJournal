using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Teacher
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<TeachersLesson> TeachersLessons { get; set; } = new List<TeachersLesson>();

    public virtual User User { get; set; } = null!;
}
