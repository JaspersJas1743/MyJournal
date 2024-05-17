using System.Diagnostics;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class StudentInTaughtClass : BaseStudent
{
	private readonly AsyncLazy<GradeOfStudent> _grade;

	private StudentInTaughtClass(
		int id,
		string surname,
		string name,
		string? patronymic,
		AsyncLazy<GradeOfStudent> grade
	) : base(id: id, surname: surname, name: name, patronymic: patronymic)
	{
		_grade = grade;
	}

	#region Events
	internal event CreatedAssessmentHandler CreatedAssessment;
	internal event ChangedAssessmentHandler ChangedAssessment;
	internal event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	public async Task<GradeOfStudent> GetGrade()
		=> await _grade;

	internal bool GradeIsCreated => _grade.IsValueCreated;

	internal static async Task<StudentInTaughtClass> Create(
		ApiClient client,
		int subjectId,
		int id,
		string surname,
		string name,
		string? patronymic,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudentInTaughtClass(
			id: id,
			surname: surname,
			name: name,
			patronymic: patronymic,
			grade: new AsyncLazy<GradeOfStudent>(valueFactory: async () => await GradeOfStudent.Create(
				client: client,
				studentId: id,
				subjectId: subjectId,
				periodId: educationPeriodId,
				cancellationToken: cancellationToken
			))
		);
	}

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		GradeOfStudent grade = await _grade;
		await grade.OnCreatedFinalAssessment(e: e);
		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		GradeOfStudent grade = await _grade;
		await grade.OnCreatedAssessment(e: e);
		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		GradeOfStudent grade = await _grade;
		await grade.OnChangedAssessment(e: e);
		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		GradeOfStudent grade = await _grade;
		await grade.OnDeletedAssessment(e: e);
		DeletedAssessment?.Invoke(e: e);
	}
}