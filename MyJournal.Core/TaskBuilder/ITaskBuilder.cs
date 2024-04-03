using MyJournal.Core.SubEntities;

namespace MyJournal.Core.TaskBuilder;

public interface ITaskBuilder
{
	ITaskBuilder AddText(string text);
	Task<ITaskBuilder> AddAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	Task<ITaskBuilder> RemoveAttachment(int index, CancellationToken cancellationToken = default(CancellationToken));
	ITaskBuilder AddReleaseDate(DateTime dateOfRelease);
	ITaskBuilder ForClass(int classId);
	ITaskBuilder ForClass(Class @class);
	ITaskBuilder ForSubject(int subjectId);
	ITaskBuilder ForSubject(Subject subject);
	Task<string> Save(CancellationToken cancellationToken = default(CancellationToken));
}