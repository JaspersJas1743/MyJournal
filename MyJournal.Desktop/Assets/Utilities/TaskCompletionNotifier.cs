using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities;

public sealed class TaskCompletionNotifier<TResult> : ReactiveObject, ITaskCompletionNotifier
{
    public TaskCompletionNotifier(Task<TResult?> task)
    {
        Task = task;
        if (task.IsCompleted)
            return;

        TaskScheduler scheduler = (SynchronizationContext.Current == null) ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext();
        task.ContinueWith(continuationAction: t =>
            {
                this.RaisePropertyChanged(propertyName: nameof(IsCompleted));
                if (t.IsCanceled)
                    this.RaisePropertyChanged(propertyName: nameof(IsCanceled));
                else if (t.IsFaulted)
                    this.RaisePropertyChanged(propertyName: nameof(IsFaulted));
                else
                {
                    this.RaisePropertyChanged(propertyName: nameof(IsSuccessfullyCompleted));
                    this.RaisePropertyChanged(propertyName: nameof(Result));
                }
            },
            cancellationToken: CancellationToken.None,
            continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
            scheduler: scheduler);
    }

    public Task<TResult?> Task { get; private set; }
    Task ITaskCompletionNotifier.Task => Task;
    public TResult? Result => (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default(TResult);
    public bool IsCompleted => Task.IsCompleted;
    public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
    public bool IsCanceled => Task.IsCanceled;
    public bool IsFaulted => Task.IsFaulted;
}

internal interface ITaskCompletionNotifier
{
    Task Task { get; }
}