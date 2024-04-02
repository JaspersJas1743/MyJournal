using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public sealed class StudentInClass : BaseStudent
{
	private readonly ApiClient _client;

	private StudentInClass(
		ApiClient client,
		int id,
		string surname,
		string name,
		string? patronymic
	) : base(id: id, surname: surname, name: name, patronymic: patronymic)
	{
		_client = client;
	}

	internal static async Task<StudentInClass> Create(
		ApiClient client,
		int id,
		string surname,
		string name,
		string? patronymic,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudentInClass(
			client: client,
			id: id,
			surname: surname,
			name: name,
			patronymic: patronymic
		);
	}

	public async Task<GradeOfStudent<EstimationOfStudent>> GetGrade(
		int subjectId,
		int periodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await GradeOfStudent<EstimationOfStudent>.Create(
			client: _client,
			studentId: Id,
			subjectId: subjectId,
			periodId: periodId,
			cancellationToken: cancellationToken
		);
	}
}