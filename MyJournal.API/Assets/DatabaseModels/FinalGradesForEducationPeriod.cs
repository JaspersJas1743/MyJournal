using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class FinalGradesForEducationPeriod
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int LessonId { get; set; }

    public int EducationPeriodId { get; set; }

    public int GradeId { get; set; }

    public virtual EducationPeriod EducationPeriod { get; set; } = null!;

    public virtual Grade Grade { get; set; } = null!;

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
