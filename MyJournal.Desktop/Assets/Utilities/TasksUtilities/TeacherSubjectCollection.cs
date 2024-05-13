using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Builders.TaskBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class TeacherSubjectCollection
{
	private readonly TaughtSubjectCollection? _taughtSubjectCollection;
	private readonly ClassCollection? _classCollection;

	public TeacherSubjectCollection(TaughtSubjectCollection taughtSubjectCollection)
	{
		_taughtSubjectCollection = taughtSubjectCollection;
		_taughtSubjectCollection.CreatedTask += e => CreatedTask?.Invoke(e: e);
	}

	public TeacherSubjectCollection(ClassCollection classCollection)
	{
		_classCollection = classCollection;
		_classCollection.CreatedTask += e => CreatedTask?.Invoke(e: e);
	}

	public event CreatedTaskHandler CreatedTask;

	public async Task<List<TeacherSubject>> ToListAsync()
	{
		if (_classCollection is not null)
		{
			return await _classCollection.SelectManyAwait(selector: async @class =>
			{
				StudyingSubjectInClassCollection a = await @class.GetStudyingSubjects();
				return a.Select(selector: studyingSubjectInClass => new TeacherSubject(
					studyingSubjectInClass: studyingSubjectInClass,
					classId: studyingSubjectInClass.Id,
					className: studyingSubjectInClass.Name?.Contains(value: "Все дисциплины") == true ? null : @class.Name
				));
			}).ToListAsync();
		}

		return new List<TeacherSubject>(collection: await Task.WhenAll(tasks: await _taughtSubjectCollection!.Select(selector: async taughtSubject =>
		{
			TaughtClass @class = await taughtSubject.GetTaughtClass();
			TeacherSubject subject = new TeacherSubject(taughtSubject: taughtSubject, classId: @class.Id, className: @class.Name);
			await subject.LoadClass();
			return subject;
		}).ToArrayAsync()));
	}

	public ITaskBuilder? CreateTask()
		=> _taughtSubjectCollection?.CreateTask();
}