using MyJournal.Core.Builders.EstimationChanger;
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
		GradeTypes gradeType,
		IEnumerable<CommentsForAssessment> commentsForAssessments
	) : base(
		id: id,
		assessment: assessment,
		createdAt: createdAt,
		comment: comment,
		description: description,
		gradeType: gradeType,
		commentsForAssessments: commentsForAssessments
	)
	{
		_client = client;
	}

	private sealed record DeleteAssessmentRequest(int AssessmentId);

	internal static async Task<EstimationOfStudent> Create(
		ApiClient client,
		int id,
		string assessment,
		DateTime createdAt,
		string? comment,
		string? description,
		GradeTypes gradeType,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<CommentsForAssessment> commentsForAssessments = await client.GetAsync<IEnumerable<CommentsForAssessment>>(
			apiMethod: AssessmentControllerMethods.GetCommentsForAssessments(assessmentId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new EstimationOfStudent(
			client: client,
			id: id,
			assessment: assessment,
			createdAt: createdAt,
			comment: comment,
			description: description,
			gradeType: gradeType,
			commentsForAssessments: commentsForAssessments
		);
	}

	public IEstimationChanger Change()
		=> Builders.EstimationChanger.EstimationChanger.Create(client: _client, estimation: this);

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