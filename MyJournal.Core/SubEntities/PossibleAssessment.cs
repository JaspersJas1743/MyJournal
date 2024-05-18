using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class PossibleAssessment : ISubEntity
{
	private readonly AsyncLazy<IEnumerable<CommentsForAssessment>> _commentsForAssessments;

	private PossibleAssessment(
		int id,
		string assessment,
		AsyncLazy<IEnumerable<CommentsForAssessment>> commentsForAssessments
	)
	{
		Id = id;
		Assessment = assessment;
		_commentsForAssessments = commentsForAssessments;
	}

	public int Id { get; init; }
	public string Assessment { get; init; }

	private sealed record GetPossibleAssessmentResponse(int Id, string Assessment);

	public async Task<IEnumerable<CommentsForAssessment>> GetComments()
		=> await _commentsForAssessments;

	public static async Task<IEnumerable<PossibleAssessment>> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetPossibleAssessmentResponse> response = await client.GetAsync<IEnumerable<GetPossibleAssessmentResponse>>(
			apiMethod: AssessmentControllerMethods.GetPossibleAssessments,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Select(selector: r => new PossibleAssessment(
			id: r.Id,
			assessment: r.Assessment,
			commentsForAssessments: new AsyncLazy<IEnumerable<CommentsForAssessment>>(valueFactory: async () => await client.GetAsync<IEnumerable<CommentsForAssessment>>(
				apiMethod: AssessmentControllerMethods.GetCommentsForAssessments(assessmentId: r.Id),
				cancellationToken: cancellationToken
			) ?? throw new InvalidOperationException()
		)));
	}
}