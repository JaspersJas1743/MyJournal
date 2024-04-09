using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Builders.EstimationChanger;

internal sealed class EstimationChanger : IEstimationChanger
{
	private readonly ApiClient _client;
	private readonly int _id;
	private int _assessmentId = -1;
	private DateTime _createdAt = DateTime.MinValue;
	private int _commentId = -1;

	private EstimationChanger(
		ApiClient client,
		int estimationId
	)
	{
		_client = client;
		_id = estimationId;
	}

	private sealed record ChangeAssessmentRequest(int ChangedAssessmentId, int NewGradeId, DateTime Datetime, int CommentId);

	public IEstimationChanger GradeTo(int newGradeId)
	{
		_assessmentId = newGradeId;
		return this;
	}

	public IEstimationChanger DatetimeTo(DateTime newDateTime)
	{
		_createdAt = newDateTime;
		return this;
	}

	public IEstimationChanger CommentTo(int newCommentId)
	{
		_commentId = newCommentId;
		return this;
	}

	public async Task Save(CancellationToken cancellationToken = default(CancellationToken))
	{
		await _client.PutAsync<ChangeAssessmentRequest>(
			apiMethod: AssessmentControllerMethods.Change,
			arg: new ChangeAssessmentRequest(
				ChangedAssessmentId: _id,
				NewGradeId: _assessmentId,
				Datetime: _createdAt,
				CommentId: _commentId
			),
			cancellationToken: cancellationToken
		);
	}

	public static EstimationChanger Create(
		ApiClient client,
		Estimation estimation
	) => new EstimationChanger(client: client, estimationId: estimation.Id);

	public static EstimationChanger Create(
		ApiClient client,
		int estimationId
	) => new EstimationChanger(client: client, estimationId: estimationId);
}