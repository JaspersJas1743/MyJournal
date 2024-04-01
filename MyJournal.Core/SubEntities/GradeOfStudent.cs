using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class GradeOfStudent<T> : Grade<T> where T: Estimation
{
	private readonly ApiClient _client;
	private readonly int _subjectId;
	private readonly int _studentId;

	private GradeOfStudent(
		ApiClient client,
		int subjectId,
		int studentId,
		AsyncLazy<List<T>> estimations,
		string average,
		string? final = null
	) : base(estimations: estimations, average: average, final: final)
	{
		_client = client;
		_subjectId = subjectId;
		_studentId = studentId;
	}

	private sealed record CreateAssessmentRequest(int GradeId, DateTime Datetime, int CommentId, int SubjectId, int StudentId);

	internal static async Task<GradeOfStudent<EstimationOfStudent>> Create(
		ApiClient client,
		int studentId,
		int subjectId,
		int periodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetAssessmentsByIdResponse assessments = await client.GetAsync<GetAssessmentsByIdResponse, GetAssessmentsRequest>(
			apiMethod: AssessmentControllerMethods.GetAssessmentsById(studentId: studentId),
			argQuery: new GetAssessmentsRequest(PeriodId: periodId, SubjectId: subjectId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new GradeOfStudent<EstimationOfStudent>(
			client: client,
			subjectId: subjectId,
			studentId: studentId,
			estimations: new AsyncLazy<List<EstimationOfStudent>>(valueFactory: async () => new List<EstimationOfStudent>(collection: await Task.WhenAll(
				tasks: assessments.Assessments.Select(selector: async e => await EstimationOfStudent.Create(
					client: client,
					id: e.Id,
					assessment: e.Assessment,
					createdAt: e.CreatedAt,
					comment: e.Comment,
					description: e.Description,
					gradeType: e.GradeType
				))
			))),
			average: assessments.AverageAssessment
		);
	}

	public async Task Add(
		int gradeId,
		DateTime dateTime,
		int commentId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PostAsync<CreateAssessmentRequest>(
			apiMethod: AssessmentControllerMethods.Create,
			arg: new CreateAssessmentRequest(GradeId: gradeId, Datetime: dateTime, CommentId: commentId, SubjectId: _subjectId, StudentId: _studentId),
			cancellationToken: cancellationToken
		);
	}
}