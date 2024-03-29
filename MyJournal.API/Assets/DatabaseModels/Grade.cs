using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Grade
{
    public int Id { get; set; }

    public string Assessment { get; set; } = null!;

    public int GradeTypeId { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<FinalGradesForEducationPeriod> FinalGradesForEducationPeriods { get; set; } = new List<FinalGradesForEducationPeriod>();

    public virtual GradeType GradeType { get; set; } = null!;
}
