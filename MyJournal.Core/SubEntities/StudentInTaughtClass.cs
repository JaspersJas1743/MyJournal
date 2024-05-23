using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class StudentInTaughtClass : BaseStudent
{
	private StudentInTaughtClass(
		int id,
		string surname,
		string name,
		string? patronymic,
		GradeOfStudent grade
	) : base(
		id: id,
		surname: surname,
		name: name,
		patronymic: patronymic
	) => Grade = grade;

	#region Events
	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	public GradeOfStudent Grade { get; }

	internal static async Task<StudentInTaughtClass> Create(
		ApiClient client,
		int subjectId,
		int id,
		string surname,
		string name,
		string? patronymic,
		GetAssessmentsByIdResponse response,
		int educationPeriodId = 0
	)
	{
		return new StudentInTaughtClass(
			id: id,
			surname: surname,
			name: name,
			patronymic: patronymic,
			grade: GradeOfStudent.Create(
				client: client,
				studentId: id,
				subjectId: subjectId,
				response: response,
				periodId: educationPeriodId
			)
		);
	}

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		await Grade.OnCreatedFinalAssessment(e: e);
		CreatedFinalAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		await Grade.OnCreatedAssessment(e: e);
		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		await Grade.OnChangedAssessment(e: e);
		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		await Grade.OnDeletedAssessment(e: e);
		DeletedAssessment?.Invoke(e: e);
	}
}