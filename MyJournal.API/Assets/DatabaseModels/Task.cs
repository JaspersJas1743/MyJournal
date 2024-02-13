using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Task
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public int ClassId { get; set; }

    public DateTime ReleasedAt { get; set; }

    public string? Text { get; set; }

    public int CreatorId { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Teacher Creator { get; set; } = null!;

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual ICollection<TaskCompletionResult> TaskCompletionResults { get; set; } = new List<TaskCompletionResult>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
