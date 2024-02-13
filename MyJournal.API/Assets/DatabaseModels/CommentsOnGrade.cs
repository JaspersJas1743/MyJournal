using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class CommentsOnGrade
{
    public int Id { get; set; }

    public string? Comment { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<GradeType> GradeTypes { get; set; } = new List<GradeType>();
}
