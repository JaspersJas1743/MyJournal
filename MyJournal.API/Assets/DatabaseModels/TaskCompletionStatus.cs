using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum TaskCompletionStatuses
{
    Completed,
    Uncompleted
}

public partial class TaskCompletionStatus
{
    public int Id { get; set; }

    public TaskCompletionStatuses CompletionStatus { get; set; }

    public virtual ICollection<TaskCompletionResult> TaskCompletionResults { get; set; } = new List<TaskCompletionResult>();
}
