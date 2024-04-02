using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class StudentInTaughtClass : BaseStudent
{
	private readonly AsyncLazy<GradeOfStudent<EstimationOfStudent>> _grade;

	private StudentInTaughtClass(
		int id,
		string surname,
		string name,
		string? patronymic,
		AsyncLazy<GradeOfStudent<EstimationOfStudent>> grade
	) : base(id: id, surname: surname, name: name, patronymic: patronymic)
	{
		_grade = grade;
	}

	public async Task<GradeOfStudent<EstimationOfStudent>> GetGrade()
		=> await _grade;

	internal static async Task<StudentInTaughtClass> Create(
		ApiClient client,
		int subjectId,
		int id,
		string surname,
		string name,
		string? patronymic,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudentInTaughtClass(
			id: id,
			surname: surname,
			name: name,
			patronymic: patronymic,
			grade: new AsyncLazy<GradeOfStudent<EstimationOfStudent>>(valueFactory: async () => await GradeOfStudent<EstimationOfStudent>.Create(
				client: client,
				studentId: id,
				subjectId: subjectId,
				cancellationToken: cancellationToken
			))
		);
	}
}