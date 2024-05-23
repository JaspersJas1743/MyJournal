using System.Threading.Tasks;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class StudentSubject : StudentSubjectBase
{
	private readonly StudyingSubject? _studyingSubject;
	private readonly WardSubjectStudying? _wardSubjectStudying;

	public StudentSubject(StudyingSubject studyingSubject)
	{
		_studyingSubject = studyingSubject;
		Id = studyingSubject.Id;
		Name = studyingSubject.Name;
		Teacher = studyingSubject.Teacher;

		studyingSubject.CompletedTask += e => CompletedTask?.Invoke(e: e);
		studyingSubject.UncompletedTask += e => UncompletedTask?.Invoke(e: e);
	}

	public StudentSubject(WardSubjectStudying wardSubjectStudying)
	{
		_wardSubjectStudying = wardSubjectStudying;

		Id = wardSubjectStudying.Id;
		Name = wardSubjectStudying.Name;
		Teacher = wardSubjectStudying.Teacher;

		wardSubjectStudying.CompletedTask += e => CompletedTask?.Invoke(e: e);
		wardSubjectStudying.UncompletedTask += e => UncompletedTask?.Invoke(e: e);
	}

	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;

	public async Task<ReceivedTaskCollection> GetTasks()
	{
		return _studyingSubject is not null
			? new ReceivedTaskCollection(assignedTaskCollection: await _studyingSubject.GetTasks())
			: new ReceivedTaskCollection(taskAssignedToWardCollection: await _wardSubjectStudying!.GetTasks());
	}
}