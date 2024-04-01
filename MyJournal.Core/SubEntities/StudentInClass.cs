using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class StudentInClass : ISubEntity
{
	private readonly AsyncLazy<GradeOfStudent<EstimationOfStudent>> _grade;

	private StudentInClass(
		int id,
		string surname,
		string name,
		string? patronymic,
		AsyncLazy<GradeOfStudent<EstimationOfStudent>> grade
	)
	{
		Id = id;
		Surname = surname;
		Name = name;
		Patronymic = patronymic;
		_grade = grade;
	}

	public int Id { get; init; }
	public string Surname { get; init; }
	public string Name { get; init; }
	public string? Patronymic { get; init; }

	public async Task<GradeOfStudent<EstimationOfStudent>> GetGrade()
		=> await _grade;

	internal static async Task<StudentInClass> Create(
		ApiClient client,
		int subjectId,
		int id,
		string surname,
		string name,
		string? patronymic,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudentInClass(
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