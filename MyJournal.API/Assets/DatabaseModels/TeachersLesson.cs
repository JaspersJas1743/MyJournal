using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class TeachersLesson
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int LessonId { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
