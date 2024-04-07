using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class LessonTiming
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int LessonId { get; set; }

    public int DayOfWeekId { get; set; }

    public int ClassId { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual DaysOfWeek DayOfWeek { get; set; } = null!;

    public virtual Lesson Lesson { get; set; } = null!;
}
