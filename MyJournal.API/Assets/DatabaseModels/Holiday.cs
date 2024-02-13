using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Holiday
{
    public int Id { get; set; }

    public int EducationPeriodId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public virtual EducationPeriod EducationPeriod { get; set; } = null!;
}
