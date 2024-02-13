using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum GradeTypes
{
    Assessment,
    Truancy
}

public partial class GradeType
{
    public int Id { get; set; }

    public GradeTypes Type { get; set; }

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<CommentsOnGrade> CommentsOnGrades { get; set; } = new List<CommentsOnGrade>();
}
