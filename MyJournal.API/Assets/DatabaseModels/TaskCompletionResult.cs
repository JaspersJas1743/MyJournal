using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class TaskCompletionResult
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public int StudentId { get; set; }

    public int TaskCompletionStatusId { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual Task Task { get; set; } = null!;

    public virtual TaskCompletionStatus TaskCompletionStatus { get; set; } = null!;
}
