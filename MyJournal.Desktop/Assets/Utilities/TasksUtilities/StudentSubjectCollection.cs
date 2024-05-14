using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class StudentSubjectCollection
{
	private readonly StudyingSubjectCollection? _studyingSubjectCollection;
	private readonly WardStudyingSubjectCollection? _wardStudyingSubjectCollection;

	public StudentSubjectCollection(StudyingSubjectCollection studyingSubjectCollection)
	{
		_studyingSubjectCollection = studyingSubjectCollection;

		_studyingSubjectCollection.CreatedTask += e => CreatedTask?.Invoke(e: e);
	}

	public StudentSubjectCollection(WardStudyingSubjectCollection wardStudyingSubjectCollection)
	{
		_wardStudyingSubjectCollection = wardStudyingSubjectCollection;

		_wardStudyingSubjectCollection.CreatedTask += e => CreatedTask?.Invoke(e: e);
	}

	public event CreatedTaskHandler CreatedTask;

	public async Task<List<StudentSubject>> ToListAsync()
	{
		if (_studyingSubjectCollection is not null)
			return await _studyingSubjectCollection.Select(selector: studyingSubject => new StudentSubject(studyingSubject: studyingSubject)).ToListAsync();

		return await _wardStudyingSubjectCollection!.Select(selector: wardSubjectStudying => new StudentSubject(wardSubjectStudying: wardSubjectStudying)).ToListAsync();
	}
}