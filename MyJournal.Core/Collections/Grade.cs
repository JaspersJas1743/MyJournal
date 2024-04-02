using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public class Grade<T> : IAsyncEnumerable<T> where T: Estimation
{
	#region Fields
	protected readonly AsyncLazy<List<T>> _estimations;

	protected string _average;
	protected string? _final;

	internal static readonly Grade<Estimation> Empty = new Grade<Estimation>(
		estimations: new AsyncLazy<List<Estimation>>(valueFactory: async () => new List<Estimation>()),
		average: String.Empty,
		final: String.Empty
	);
	#endregion

	#region Constructors
	protected Grade(
		AsyncLazy<List<T>> estimations,
		string average,
		string? final = null
	)
	{
		_estimations = estimations;
		_average = average;
		_final = final;
	}
	#endregion

	#region Properties
	public string AverageAssessment => _average;

	public string? FinalAssessment => _final;
	#endregion

	#region Records
	protected sealed record GetAssessmentsRequest(int PeriodId, int SubjectId);
	protected sealed record EstimationResponse(int Id, string Assessment, DateTime CreatedAt, string? Comment, string? Description, GradeTypes GradeType);
	protected sealed record GetAssessmentsResponse(string AverageAssessment, string? FinalAssessment, IEnumerable<EstimationResponse> Assessments);
	protected sealed record GetAssessmentsByIdResponse(string AverageAssessment, IEnumerable<EstimationResponse> Assessments);
	#endregion

	#region Methods
	#region Static
	internal static async Task<Grade<Estimation>> Create(
		ApiClient client,
		string apiMethod,
		int subjectId,
		int periodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetAssessmentsResponse assessments = await client.GetAsync<GetAssessmentsResponse, GetAssessmentsRequest>(
			apiMethod: apiMethod,
			argQuery: new GetAssessmentsRequest(PeriodId: periodId, SubjectId: subjectId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new Grade<Estimation>(
			estimations: new AsyncLazy<List<Estimation>>(valueFactory: async () => new List<Estimation>(collection: await Task.WhenAll(
				tasks: assessments.Assessments.Select(selector: async e => await Estimation.Create(
					id: e.Id,
					assessment: e.Assessment,
					createdAt: e.CreatedAt,
					comment: e.Comment,
					description: e.Description,
					gradeType: e.GradeType
				))
			))),
			average: assessments.AverageAssessment,
			final: assessments.FinalAssessment
		);
	}
	#endregion

	#region Instance
	public async Task<IEnumerable<Estimation>> GetAssessments()
		=> await _estimations;
	#endregion

	#region IAsyncEnumerable<Estimation>
	public async IAsyncEnumerator<T> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (T estimation in await _estimations)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return estimation;
		}
	}
	#endregion
	#endregion
}