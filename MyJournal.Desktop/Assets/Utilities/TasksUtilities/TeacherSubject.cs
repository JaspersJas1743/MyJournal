using System.Threading.Tasks;
using MyJournal.Core.Builders.TaskBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class TeacherSubject : TeacherSubjectBase
{
	private readonly TaughtSubject? _taughtSubject;
	private readonly StudyingSubjectInClass? _studyingSubjectInClass;

	public TeacherSubject(TaughtSubject taughtSubject, int classId, string? className)
	{
		_taughtSubject = taughtSubject;
		Id = taughtSubject.Id;
		Name = taughtSubject.Name;
		ClassName = className;
		ClassId = classId;

		taughtSubject.CompletedTask += e => CompletedTask?.Invoke(e: e);
		taughtSubject.UncompletedTask += e => UncompletedTask?.Invoke(e: e);
	}

	public TeacherSubject(StudyingSubjectInClass studyingSubjectInClass, int classId, string? className)
	{
		_studyingSubjectInClass = studyingSubjectInClass;

		Id = studyingSubjectInClass.Id;
		Name = studyingSubjectInClass.Name;
		ClassId = classId;
		ClassName = className;

		studyingSubjectInClass.CompletedTask += e => CompletedTask?.Invoke(e: e);
		studyingSubjectInClass.UncompletedTask += e => UncompletedTask?.Invoke(e: e);
	}

	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;

	public async Task LoadClass()
		=> ClassName = (await _taughtSubject?.GetTaughtClass()!)?.Name ?? ClassName;

	public async Task<CreatedTaskCollection> GetTasks()
	{
		if (_taughtSubject is not null)
			return new CreatedTaskCollection(createdTaskCollection: await _taughtSubject.GetTasks());

		return new CreatedTaskCollection(taskAssignedToClassCollection: await _studyingSubjectInClass!.GetTasks());
	}

	public ITaskBuilder? CreateTask()
		=> _taughtSubject?.CreateTask()!;
}