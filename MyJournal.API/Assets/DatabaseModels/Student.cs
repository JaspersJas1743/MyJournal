using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Student
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ClassId { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<FinalGradesForEducationPeriod> FinalGradesForEducationPeriods { get; set; } = new List<FinalGradesForEducationPeriod>();

    public virtual ICollection<Parent> Parents { get; set; } = new List<Parent>();

    public virtual ICollection<TaskCompletionResult> TaskCompletionResults { get; set; } = new List<TaskCompletionResult>();

    public virtual User User { get; set; } = null!;
}
