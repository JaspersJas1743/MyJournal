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
		GradeTypes gradeType,
		AsyncLazy<IEnumerable<CommentsForAssessment>> commentsForAssessments
	)
	{
		Id = id;
		Assessment = assessment;
		GradeType = gradeType;
		_commentsForAssessments = commentsForAssessments;
	}

	public int Id { get; init; }
	public string Assessment { get; init; }
	public GradeTypes GradeType { get; init; }

	private sealed record AssessmentComment(int Id, string? Comment, string Description);
	private sealed record GetPossibleAssessmentResponse(int Id, string Assessment, GradeTypes GradeType, IEnumerable<AssessmentComment> Comments);

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
			gradeType: r.GradeType,
			commentsForAssessments: new AsyncLazy<IEnumerable<CommentsForAssessment>>(
				valueFactory: async () => r.Comments.Select(selector: c => new CommentsForAssessment(
					Id: c.Id,
					Comment: c.Comment,
					Description: c.Description
				)
			))
		));
	}
}