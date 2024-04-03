using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public enum GradeTypes
{
	Assessment,
	Truancy
}

public sealed record CommentsForAssessment(int Id, string? Comment, string Description);

public class Estimation
{
	protected Estimation(
		int id,
		string assessment,
		DateTime createdAt,
		string? comment,
		string? description,
		GradeTypes gradeType,
		IEnumerable<CommentsForAssessment> commentsForAssessments
	)
	{
		Id = id;
		Assessment = assessment;
		CreatedAt = createdAt;
		Comment = comment;
		Description = description;
		GradeType = gradeType;
		CommentsForAssessments = commentsForAssessments;
	}

	public int Id { get; internal set; }
	public string Assessment { get; internal set; }
	public DateTime CreatedAt { get; internal set; }
	public string? Comment { get; internal set; }
	public string? Description { get; internal set; }
	public GradeTypes GradeType { get; internal set; }
	public IEnumerable<CommentsForAssessment> CommentsForAssessments { get; init; }

	internal static async Task<Estimation> Create(
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
		return new Estimation(
			id: id,
			assessment: assessment,
			createdAt: createdAt,
			comment: comment,
			description: description,
			gradeType: gradeType,
			commentsForAssessments: commentsForAssessments
		);
	}
}