using MyJournal.Core.Builders.TaskBuilder;
using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class Subject
{
	private readonly TaughtSubject? _taughtSubject;
	private readonly StudyingSubjectInClass? _studyingSubjectInClass;

	public Subject(TaughtSubject subject)
		=> _taughtSubject = subject;

	public Subject(StudyingSubjectInClass subject)
		=> _studyingSubjectInClass = subject;

	public int Id => _taughtSubject?.Id ?? _studyingSubjectInClass!.Id;
	public string? Name => _taughtSubject?.Name ?? _studyingSubjectInClass!.Name;

	public ITaskBuilder? Create()
		=> _taughtSubject?.CreateTask();
}