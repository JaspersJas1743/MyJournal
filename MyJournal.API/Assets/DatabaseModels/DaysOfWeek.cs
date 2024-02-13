using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class DaysOfWeek
{
    public int Id { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public int TypeOfDay { get; set; }

    public virtual ICollection<LessonTiming> LessonTimings { get; set; } = new List<LessonTiming>();

    public virtual TypeOfDay TypeOfDayNavigation { get; set; } = null!;
}
