using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Assessment
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public DateTime Datetime { get; set; }

    public int StudentId { get; set; }

    public int GradeId { get; set; }

    public int? CommentId { get; set; }

    public virtual CommentsOnGrade? Comment { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
