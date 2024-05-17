using MyJournal.Core.Builders.EstimationBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class GradeOfStudent : Grade<EstimationOfStudent>
{
	private readonly AsyncLazy<IEnumerable<PossibleAssessment>> _possibleAssessments;
	private readonly int _subjectId;
	private readonly int _studentId;
	private int? _final;

	private GradeOfStudent(
		ApiClient client,
		int subjectId,
		int studentId,
		string average,
		int periodId,
		AsyncLazy<List<EstimationOfStudent>> estimations,
		AsyncLazy<IEnumerable<PossibleAssessment>> possibleAssessments,
		int? final = null
	) : base(client: client, estimations: estimations, average: average, periodId: periodId)
	{
		_subjectId = subjectId;
		_studentId = studentId;
		_possibleAssessments = possibleAssessments;
		_final = final;
	}

	private sealed record CreateFinalAssessmentRequest(int GradeId, int SubjectId, int StudentId);

	internal static async Task<GradeOfStudent> Create(
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
		return new GradeOfStudent(
			client: client,
			subjectId: subjectId,
			studentId: studentId,
			periodId: periodId,
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
			possibleAssessments: new AsyncLazy<IEnumerable<PossibleAssessment>>(
				valueFactory: async () => await client.GetAsync<IEnumerable<PossibleAssessment>>(
					apiMethod: AssessmentControllerMethods.GetPossibleAssessments,
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException()
			),
			average: assessments.AverageAssessment,
			final: assessments.FinalAssessment
		);
	}

	public IEstimationBuilder Add()
		=> EstimationBuilder.Create(client: _client, studentId: _studentId, subjectId: _subjectId);

	public async Task AddFinal(int gradeId, CancellationToken cancellationToken = default(CancellationToken))
	{
		await _client.PostAsync<CreateFinalAssessmentRequest>(
			apiMethod: AssessmentControllerMethods.CreateFinal,
			arg: new CreateFinalAssessmentRequest(GradeId: gradeId, SubjectId: _subjectId, StudentId: _studentId),
			cancellationToken: cancellationToken
		);
	}

	public async Task<IEnumerable<PossibleAssessment>> GetPossibleAssessments()
		=> await _possibleAssessments;

	internal new async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		GetFinalAssessmentByIdResponse response = await _client.GetAsync<GetFinalAssessmentByIdResponse>(
			apiMethod: e.ApiMethodForFinalAT(arg: _studentId)
		) ?? throw new InvalidOperationException();
		_final = response.FinalAssessment;
		InvokeCreatedFinalAssessment(e: e);
	}

	internal new async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		GetAssessmentResponse response = await _client.GetAsync<GetAssessmentResponse>(
			apiMethod: e.ApiMethod
		) ?? throw new InvalidOperationException();
		_average = response.AverageAssessment;
		List<EstimationOfStudent> estimations = await _estimations;
		estimations.Add(item: await EstimationOfStudent.Create(
			client: _client,
			id: response.Assessment.Id,
			assessment: response.Assessment.Assessment,
			createdAt: response.Assessment.CreatedAt,
			comment: response.Assessment.Comment,
			description: response.Assessment.Description,
			gradeType: response.Assessment.GradeType
		));
		InvokeCreatedAssessment(e: e);
	}
}