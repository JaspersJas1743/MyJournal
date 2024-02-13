using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class EducationPeriod
{
    public int Id { get; set; }

    public string Period { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public virtual ICollection<EducationPeriodForClass> EducationPeriodForClasses { get; set; } = new List<EducationPeriodForClass>();

    public virtual ICollection<FinalGradesForEducationPeriod> FinalGradesForEducationPeriods { get; set; } = new List<FinalGradesForEducationPeriod>();

    public virtual ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();
}
