using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class StudentSubjectCollection
{
	private readonly StudyingSubjectCollection? _studyingSubjectCollection;
	private readonly WardStudyingSubjectCollection? _wardStudyingSubjectCollection;

	public StudentSubjectCollection(StudyingSubjectCollection studyingSubjectCollection)
	{
		_studyingSubjectCollection = studyingSubjectCollection;

		_studyingSubjectCollection.CreatedFinalAssessment += e => CreatedFinalAssessment?.Invoke(e: e);
		_studyingSubjectCollection.CreatedAssessment += e => CreatedAssessment?.Invoke(e: e);
		_studyingSubjectCollection.DeletedAssessment += e => DeletedAssessment?.Invoke(e: e);
		_studyingSubjectCollection.ChangedAssessment += e => ChangedAssessment?.Invoke(e: e);
	}

	public StudentSubjectCollection(WardStudyingSubjectCollection wardStudyingSubjectCollection)
	{
		_wardStudyingSubjectCollection = wardStudyingSubjectCollection;

		_wardStudyingSubjectCollection.CreatedFinalAssessment += e => CreatedFinalAssessment?.Invoke(e: e);
		_wardStudyingSubjectCollection.CreatedAssessment += e => CreatedAssessment?.Invoke(e: e);
		_wardStudyingSubjectCollection.DeletedAssessment += e => DeletedAssessment?.Invoke(e: e);
		_wardStudyingSubjectCollection.ChangedAssessment += e => ChangedAssessment?.Invoke(e: e);
	}

	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;

	public async Task<StudentSubject> FindById(int id)
	{
		return _studyingSubjectCollection is not null
			? new StudentSubject(studyingSubject: await _studyingSubjectCollection.GetById(id: id))
			: new StudentSubject(wardSubjectStudying: await _wardStudyingSubjectCollection!.GetById(id: id));
	}

	public async Task<IEnumerable<EducationPeriod>> GetEducationPeriods()
	{
		return _studyingSubjectCollection is not null
			? await _studyingSubjectCollection.GetEducationPeriods()
			: await _wardStudyingSubjectCollection!.GetEducationPeriods();
	}

	public async Task<List<StudentSubject>> ToListAsync()
	{
		if (_studyingSubjectCollection is not null)
			return await _studyingSubjectCollection.Select(selector: studyingSubject => new StudentSubject(studyingSubject: studyingSubject)).ToListAsync();

		return await _wardStudyingSubjectCollection!.Select(selector: wardSubjectStudying => new StudentSubject(wardSubjectStudying: wardSubjectStudying)).ToListAsync();
	}
}