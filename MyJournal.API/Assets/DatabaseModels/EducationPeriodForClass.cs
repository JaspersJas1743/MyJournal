using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class EducationPeriodForClass
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int EducationPeriodId { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual EducationPeriod EducationPeriod { get; set; } = null!;

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
