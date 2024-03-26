using MyJournal.Core.SubEntities;

namespace MyJournal.Core.TaskBuilder;

public interface IInitTaskBuilder
{
	ITaskBuilder WithText(string text);
	Task<ITaskBuilder> WithAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	ITaskBuilder WithReleaseDate(DateTime dateOfRelease);
	ITaskBuilder ForClass(int classId);
	ITaskBuilder ForClass(Class @class);
	ITaskBuilder ForSubject(int subjectId);
	ITaskBuilder ForSubject(Subject subject);
}