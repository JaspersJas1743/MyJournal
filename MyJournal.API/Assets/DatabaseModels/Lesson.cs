using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Lesson
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<FinalGradesForEducationPeriod> FinalGradesForEducationPeriods { get; set; } = new List<FinalGradesForEducationPeriod>();

    public virtual ICollection<LessonTiming> LessonTimings { get; set; } = new List<LessonTiming>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<TeachersLesson> TeachersLessons { get; set; } = new List<TeachersLesson>();

    public virtual ICollection<EducationPeriodForClass> EducationPeriodForClasses { get; set; } = new List<EducationPeriodForClass>();
}
