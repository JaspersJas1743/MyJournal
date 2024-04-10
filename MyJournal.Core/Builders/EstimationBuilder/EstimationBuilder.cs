using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Builders.EstimationBuilder;

public sealed class EstimationBuilder : IEstimationBuilder
{
	private readonly ApiClient _client;
	private readonly int _studentId;
	private readonly int _subjectId;

	private int _gradeId = -1;
	private DateTime _creationDate = DateTime.MinValue;
	private int _commentId = -1;

	private EstimationBuilder(
		ApiClient client,
		int studentId,
		int subjectId
	)
	{
		_client = client;
		_studentId = studentId;
		_subjectId = subjectId;
	}

	private sealed record CreateAssessmentRequest(int GradeId, DateTime Datetime, int CommentId, int SubjectId, int StudentId);

	public IEstimationBuilder WithGrade(int gradeId)
	{
		_gradeId = gradeId;
		return this;
	}

	public IEstimationBuilder WithCreationDate(DateTime creationDate)
	{
		_creationDate = creationDate;
		return this;
	}

	public IEstimationBuilder WithComment(int commentId)
	{
		_commentId = commentId;
		return this;
	}

	public async Task Save(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_gradeId == -1)
			throw new ArgumentException(message: "Оценка не установлена.", paramName: nameof(_gradeId));

		if (_commentId == -1)
			throw new ArgumentException(message: "Комментарий не установлен.", paramName: nameof(_commentId));

		if (_creationDate == DateTime.MinValue)
			throw new ArgumentException(message: "Дата для оценки не установлена.", paramName: nameof(_creationDate));

		await _client.PostAsync<CreateAssessmentRequest>(
			apiMethod: AssessmentControllerMethods.Create,
			arg: new CreateAssessmentRequest(
				GradeId: _gradeId,
				Datetime: _creationDate,
				CommentId: _commentId,
				SubjectId: _subjectId,
				StudentId: _studentId
			),
			cancellationToken: cancellationToken
		);
	}

	public static EstimationBuilder Create(
		ApiClient client,
		int studentId,
		int subjectId
	) => new EstimationBuilder(client: client, studentId: studentId, subjectId: subjectId);
}