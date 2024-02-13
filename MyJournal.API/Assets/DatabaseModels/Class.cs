using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Class
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<EducationPeriodForClass> EducationPeriodForClasses { get; set; } = new List<EducationPeriodForClass>();

    public virtual ICollection<LessonTiming> LessonTimings { get; set; } = new List<LessonTiming>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<TeachersLesson> TeachersLessons { get; set; } = new List<TeachersLesson>();
}
