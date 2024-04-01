using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class EstimationOfStudent : Estimation
{
	private readonly ApiClient _client;

	private EstimationOfStudent(
		ApiClient client,
		int id,
		string assessment,
		DateTime createdAt,
		string? comment,
		string? description,
		GradeTypes gradeType
	) : base(
		id: id,
		assessment: assessment,
		createdAt: createdAt,
		comment: comment,
		description: description,
		gradeType: gradeType
	)
	{
		_client = client;
	}

	private sealed record ChangeAssessmentRequest(int ChangedAssessmentId, int NewGradeId, DateTime Datetime, int CommentId);
	private sealed record DeleteAssessmentRequest(int AssessmentId);

	internal static async Task<EstimationOfStudent> Create(
		ApiClient client,
		int id,
		string assessment,
		DateTime createdAt,
		string? comment,
		string? description,
		GradeTypes gradeType
	)
	{
		return new EstimationOfStudent(
			client: client,
			id: id,
			assessment: assessment,
			createdAt: createdAt,
			comment: comment,
			description: description,
			gradeType: gradeType
		);
	}

	public async Task Change(
		int gradeId,
		DateTime dateTime,
		int commentId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PutAsync<ChangeAssessmentRequest>(
			apiMethod: AssessmentControllerMethods.Change,
			arg: new ChangeAssessmentRequest(ChangedAssessmentId: Id, NewGradeId: gradeId, Datetime: dateTime, CommentId: commentId),
			cancellationToken: cancellationToken
		);
	}

	public async Task Delete(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.DeleteAsync<DeleteAssessmentRequest>(
			apiMethod: AssessmentControllerMethods.Delete,
			arg: new DeleteAssessmentRequest(AssessmentId: Id),
			cancellationToken: cancellationToken
		);
	}
}