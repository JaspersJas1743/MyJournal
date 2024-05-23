using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.Collections;

public sealed record EstimationResponse(int Id, string Assessment, DateTime CreatedAt, string? Comment, string? Description, GradeTypes GradeType);
internal sealed record GetAssessmentsByIdResponse(string AverageAssessment, int? FinalAssessment, IEnumerable<EstimationResponse> Assessments);

public class Grade<T> : IAsyncEnumerable<T> where T: Estimation
{
	#region Fields
	protected readonly string _apiMethod;
	protected readonly ApiClient _client;
	protected AsyncLazy<List<T>> _estimations;

	private readonly int _subjectId;
	protected int _periodId;
	protected string? _final;
	protected string _average;

	internal static readonly Grade<Estimation> Empty = new Grade<Estimation>();
	#endregion

	#region Constructors
	protected Grade() { }

	protected Grade(
		ApiClient client,
		AsyncLazy<List<T>> estimations,
		string apiMethod,
		string average,
		int subjectId,
		int periodId,
		string? final = null
	)
	{
		_apiMethod = apiMethod;
		_client = client;
		_estimations = estimations;
		_average = average;
		_periodId = periodId;
		_subjectId = subjectId;
		_final = final;
	}
	#endregion

	#region Properties
	public string AverageAssessment => _average;
	public string? FinalAssessment => _final;
	protected int? PeriodId => _periodId;
	internal bool EstimationsAreCreated => _estimations.IsValueCreated;
	#endregion

	#region Events
	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	#region Records
	protected sealed record GetAverageAssessmentRequest(int SubjectId, int PeriodId);
	protected sealed record GetAverageAssessmentResponse(string AverageAssessment);
	protected sealed record GetFinalAssessmentRequest(int SubjectId, int PeriodId);
	protected sealed record GetFinalAssessmentResponse(string FinalAssessment);
	protected sealed record GetAssessmentsRequest(int PeriodId, int SubjectId);
	protected sealed record GetAssessmentsResponse(string AverageAssessment, string? FinalAssessment, IEnumerable<EstimationResponse> Assessments);
	protected sealed record GetAssessmentResponse(string AverageAssessment, EstimationResponse Assessment, int PeriodId);
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
			apiMethod: apiMethod,
			estimations: new AsyncLazy<List<Estimation>>(valueFactory: async () =>
			{
				List<Estimation> list = new List<Estimation>(collection: assessments.Assessments.Select(
					selector: e => EstimationOfStudent.Create(
						client: client,
						id: e.Id,
						assessment: e.Assessment,
						createdAt: e.CreatedAt,
						comment: e.Comment,
						description: e.Description,
						gradeType: e.GradeType
					))
				);
				list.Sort(comparison: (first, second) => 0 - first.CreatedAt.CompareTo(value: second.CreatedAt));
				return list;
			}),
			average: assessments.AverageAssessment,
			periodId: periodId,
			subjectId: subjectId,
			final: assessments.FinalAssessment
		);
	}
	#endregion

	#region Instance
	public async Task<IEnumerable<T>> GetEstimations()
		=> await _estimations;

	public virtual async Task SetEducationPeriod(int educationPeriodId)
	{
		_periodId = educationPeriodId;
		GetAssessmentsResponse assessments = await _client.GetAsync<GetAssessmentsResponse, GetAssessmentsRequest>(
			apiMethod: _apiMethod,
			argQuery: new GetAssessmentsRequest(PeriodId: _periodId, SubjectId: _subjectId)
		) ?? throw new InvalidOperationException();
		_estimations = new AsyncLazy<List<T>>(valueFactory: async () =>
		{
			List<T> list = new List<T>(collection: assessments.Assessments.Select(selector: e => (T)Estimation.Create(
				id: e.Id,
				assessment: e.Assessment,
				createdAt: e.CreatedAt,
				comment: e.Comment,
				description: e.Description,
				gradeType: e.GradeType
			)));
			list.Sort(comparison: (first, second) => 0 - first.CreatedAt.CompareTo(value: second.CreatedAt));
			return list;
		});
		_average = assessments.AverageAssessment;
		_final = assessments.FinalAssessment;
	}

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		GetFinalAssessmentResponse response = await _client.GetAsync<GetFinalAssessmentResponse, GetFinalAssessmentRequest>(
			apiMethod: e.ApiMethodForFinalSP,
			argQuery: new GetFinalAssessmentRequest(SubjectId: _subjectId, PeriodId: _periodId)
		) ?? throw new InvalidOperationException();

		_final = response.FinalAssessment;
		CreatedFinalAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		GetAssessmentResponse response = await _client.GetAsync<GetAssessmentResponse, GetFinalAssessmentRequest>(
			apiMethod: e.ApiMethod,
			argQuery: new GetFinalAssessmentRequest(SubjectId: _subjectId, PeriodId: _periodId)
		) ?? throw new InvalidOperationException();

		if (response.PeriodId == _periodId)
			return;

		_average = response.AverageAssessment;
		List<T> estimations = await _estimations;
		estimations.Add(item: (T)Estimation.Create(
			id: response.Assessment.Id,
			assessment: response.Assessment.Assessment,
			createdAt: response.Assessment.CreatedAt,
			comment: response.Assessment.Comment,
			description: response.Assessment.Description,
			gradeType: response.Assessment.GradeType
		));
		estimations.Sort(comparison: (first, second) => 0 - first.CreatedAt.CompareTo(value: second.CreatedAt));
		CreatedAssessment?.Invoke(e: e);
	}

	protected void InvokeCreatedAssessment(CreatedAssessmentEventArgs e)
		=> CreatedAssessment?.Invoke(e: e);

	protected void InvokeCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
		=> CreatedFinalAssessment?.Invoke(e: e);

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		GetAssessmentResponse response = await _client.GetAsync<GetAssessmentResponse>(
			apiMethod: e.ApiMethod
		) ?? throw new InvalidOperationException();
		_average = response.AverageAssessment;
		List<T> estimations = await _estimations;
		Estimation? estimation = estimations.SingleOrDefault(predicate: estimation => estimation.Id == e.AssessmentId);

		if (estimation is null)
			return;

		estimation.Id = response.Assessment.Id;
		estimation.Assessment = response.Assessment.Assessment;
		estimation.CreatedAt = response.Assessment.CreatedAt;
		estimation.Comment = response.Assessment.Comment;
		estimation.Description = response.Assessment.Description;
		estimation.GradeType = response.Assessment.GradeType;
		estimation.OnChangedAssessment(e: e);
		estimations.Sort(comparison: (first, second) => 0 - first.CreatedAt.CompareTo(value: second.CreatedAt));
		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		List<T> estimations = await _estimations;
		estimations.RemoveAll(match: estimation => estimation.Id == e.AssessmentId);

		GetAverageAssessmentResponse response = await _client.GetAsync<GetAverageAssessmentResponse, GetAverageAssessmentRequest>(
			apiMethod: e.ApiMethod,
			argQuery: new GetAverageAssessmentRequest(SubjectId: e.SubjectId, PeriodId: _periodId)
		) ?? throw new InvalidOperationException();
		_average = response.AverageAssessment;
		estimations.Sort(comparison: (first, second) => 0 - first.CreatedAt.CompareTo(value: second.CreatedAt));
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