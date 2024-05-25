using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class SubjectCollection
{
	private readonly StudyingSubjectCollection? _studyingSubjectCollection;
	private readonly WardStudyingSubjectCollection? _wardStudyingSubjectCollection;
	private readonly TaughtSubjectCollection? _taughtSubjectCollection;

	public SubjectCollection(StudyingSubjectCollection? studyingSubjectCollection)
		=> _studyingSubjectCollection = studyingSubjectCollection;

	public SubjectCollection(WardStudyingSubjectCollection? wardStudyingSubjectCollection)
		=> _wardStudyingSubjectCollection = wardStudyingSubjectCollection;

	public SubjectCollection(TaughtSubjectCollection? taughtSubjectCollection)
		=> _taughtSubjectCollection = taughtSubjectCollection;

	public async Task<List<Subject>> ToListAsync()
	{
		if (_studyingSubjectCollection is not null)
			return await _studyingSubjectCollection.Select(selector: studyingSubject => new Subject(studyingSubject: studyingSubject)).ToListAsync();

		if (_wardStudyingSubjectCollection is not null)
			return await _wardStudyingSubjectCollection!.Select(selector: wardSubjectStudying => new Subject(wardSubjectStudying: wardSubjectStudying)).ToListAsync();

		return await _taughtSubjectCollection!.SelectAwait(selector: async taughtSubject =>
		{
			TaughtClass @class = await taughtSubject.GetTaughtClass();
			Subject subject = new Subject(taughtSubject: taughtSubject, classId: @class.Id, className: @class.Name);
			return subject;
		}).ToListAsync();
	}

}