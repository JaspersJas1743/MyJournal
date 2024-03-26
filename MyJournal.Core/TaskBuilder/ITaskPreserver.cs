namespace MyJournal.Core.TaskBuilder;

public interface ITaskPreserver
{
	Task<string> Save(CancellationToken cancellationToken = default(CancellationToken));
}