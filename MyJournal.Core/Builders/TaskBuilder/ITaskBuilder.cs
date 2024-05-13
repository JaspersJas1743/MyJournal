using MyJournal.Core.SubEntities;

namespace MyJournal.Core.Builders.TaskBuilder;

public interface ITaskBuilder
{
	ITaskBuilder SetText(string? text);
	Task<ITaskBuilder> AddAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	Task<ITaskBuilder> RemoveAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	ITaskBuilder SetDateOfRelease(DateTime dateOfRelease);
	ITaskBuilder SetClass(int classId);
	ITaskBuilder SetClass(Class @class);
	ITaskBuilder SetSubject(int subjectId);
	ITaskBuilder SetSubject(Subject subject);
	Task<string> Save(CancellationToken cancellationToken = default(CancellationToken));
}