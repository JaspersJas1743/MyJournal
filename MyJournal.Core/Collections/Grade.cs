using System.Diagnostics;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.Collections;

public class Grade<T> : IAsyncEnumerable<T> where T: Estimation
{
	#region Fields
	protected readonly ApiClient _client;
	protected readonly AsyncLazy<List<T>> _estimations;

	protected string _average;
	protected string? _final;

	internal static readonly Grade<Estimation> Empty = new Grade<Estimation>(
		client: null,
		estimations: new AsyncLazy<List<Estimation>>(valueFactory: async () => new List<Estimation>()),
		average: String.Empty,
		final: String.Empty
	);
	#endregion

	#region Constructors
	protected Grade(
		ApiClient client,
		AsyncLazy<List<T>> estimations,
		string average,
		string? final = null
	)
	{
		_client = client;
		_estimations = estimations;
		_average = average;
		_final = final;
	}
	#endregion

	#region Properties
	public string AverageAssessment => _average;
	public string? FinalAssessment => _final;
	internal bool EstimationsAreCreated => _estimations.IsValueCreated;
	#endregion

	#region Delegates
	public delegate void CreatedAssessmentHandler(CreatedAssessmentEventArgs e);
	public delegate void ChangedAssessmentHandler(ChangedAssessmentEventArgs e);
	public delegate void DeletedAssessmentHandler(DeletedAssessmentEventArgs e);
	#endregion

	#region Events
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	#region Records
	protected sealed record GetAverageAssessmentRequest(int SubjectId);
	protected sealed record GetAverageAssessmentResponse(string AverageAssessment);
	protected sealed record GetAssessmentsRequest(int PeriodId, int SubjectId);
	protected sealed record EstimationResponse(int Id, string Assessment, DateTime CreatedAt, string? Comment, string? Description, GradeTypes GradeType);
	protected sealed record GetAssessmentsResponse(string AverageAssessment, string? FinalAssessment, IEnumerable<EstimationResponse> Assessments);
	protected sealed record GetAssessmentsByIdResponse(string AverageAssessment, IEnumerable<EstimationResponse> Assessments);
	protected sealed record GetAssessmentResponse(string AverageAssessment, EstimationResponse Assessment);
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
			client: client,
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

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		GetAssessmentResponse response = await _client.GetAsync<GetAssessmentResponse>(
			apiMethod: e.ApiMethod
		) ?? throw new InvalidOperationException();

		_average = response.AverageAssessment;
		List<T> estimations = await _estimations;
		estimations.Add(item: (T)await Estimation.Create(
			id: response.Assessment.Id,
			assessment: response.Assessment.Assessment,
			createdAt: response.Assessment.CreatedAt,
			comment: response.Assessment.Comment,
			description: response.Assessment.Description,
			gradeType: response.Assessment.GradeType
		));
		CreatedAssessment?.Invoke(e: e);
	}

	protected void InvokeCreatedAssessment(CreatedAssessmentEventArgs e)
		=> CreatedAssessment?.Invoke(e: e);

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		GetAssessmentResponse response = await _client.GetAsync<GetAssessmentResponse>(
			apiMethod: e.ApiMethod
		) ?? throw new InvalidOperationException();
		_average = response.AverageAssessment;
		List<T> estimations = await _estimations;
		T estimation = estimations.Single(predicate: estimation => estimation.Id == e.AssessmentId);
		estimation.Id = response.Assessment.Id;
		estimation.Assessment = response.Assessment.Assessment;
		estimation.CreatedAt = response.Assessment.CreatedAt;
		estimation.Comment = response.Assessment.Comment;
		estimation.Description = response.Assessment.Description;
		estimation.GradeType = response.Assessment.GradeType;

		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		List<T> estimations = await _estimations;
		estimations.RemoveAll(match: estimation => estimation.Id == e.AssessmentId);

		GetAverageAssessmentResponse response = await _client.GetAsync<GetAverageAssessmentResponse, GetAverageAssessmentRequest>(
			apiMethod: e.ApiMethod,
			argQuery: new GetAverageAssessmentRequest(SubjectId: e.SubjectId)
		) ?? throw new InvalidOperationException();

		_average = response.AverageAssessment;

		DeletedAssessment?.Invoke(e: e);
	}
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