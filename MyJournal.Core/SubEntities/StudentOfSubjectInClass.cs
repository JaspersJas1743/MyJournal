using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class StudentOfSubjectInClass : BaseStudent
{
	private readonly ApiClient _client;
	private readonly AsyncLazy<GradeOfStudent<EstimationOfStudent>> _grade;
	private readonly int _subjectId;

	private StudentOfSubjectInClass(
		ApiClient client,
		int id,
		string surname,
		string name,
		string? patronymic,
		AsyncLazy<GradeOfStudent<EstimationOfStudent>> grade,
		int subjectId
	) : base(id: id, surname: surname, name: name, patronymic: patronymic)
	{
		_client = client;
		_grade = grade;
		_subjectId = subjectId;
	}

	internal static async Task<StudentOfSubjectInClass> Create(
		ApiClient client,
		int id,
		string surname,
		string name,
		string? patronymic,
		int subjectId,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudentOfSubjectInClass(
			client: client,
			id: id,
			surname: surname,
			name: name,
			patronymic: patronymic,
			subjectId: subjectId,
			grade: new AsyncLazy<GradeOfStudent<EstimationOfStudent>>(valueFactory: async () => await GradeOfStudent<EstimationOfStudent>.Create(
				client: client,
				studentId: id,
				subjectId: subjectId,
				periodId: educationPeriodId,
				cancellationToken: cancellationToken
			))
		);
	}

	public async Task<GradeOfStudent<EstimationOfStudent>> GetGrade()
		=> await _grade;
}