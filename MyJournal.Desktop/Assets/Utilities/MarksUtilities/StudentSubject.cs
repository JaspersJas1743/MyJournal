using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

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

		_studyingSubject.CreatedAssessment += e => CreatedAssessment?.Invoke(e: e);
		_studyingSubject.CreatedFinalAssessment += e => CreatedFinalAssessment?.Invoke(e: e);
	}

	public StudentSubject(WardSubjectStudying wardSubjectStudying)
	{
		_wardSubjectStudying = wardSubjectStudying;
		Id = wardSubjectStudying.Id;
		Name = wardSubjectStudying.Name;
		Teacher = wardSubjectStudying.Teacher;

		_wardSubjectStudying.CreatedAssessment += e => CreatedAssessment?.Invoke(e: e);
		_wardSubjectStudying.CreatedFinalAssessment += e => CreatedFinalAssessment?.Invoke(e: e);
	}

	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;

	public async Task<Grade<Estimation>> GetGrade()
	{
		return _studyingSubject is not null
			? await _studyingSubject.GetGrade()
			: await _wardSubjectStudying!.GetGrade();
	}
}