using MyJournal.Core.Builders.EstimationBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class GradeOfStudent : Grade<EstimationOfStudent>
{
	private readonly int _subjectId;
	private readonly int _studentId;

	private GradeOfStudent(
		ApiClient client,
		string apiMethod,
		int subjectId,
		int studentId,
		string average,
		int periodId,
		AsyncLazy<List<EstimationOfStudent>> estimations,
		int? final = null
	) : base(
		client: client,
		estimations: estimations,
		apiMethod: apiMethod,
		average: average,
		subjectId: subjectId,
		periodId: periodId
	)
	{
		_subjectId = subjectId;
		_studentId = studentId;
		FinalAssessment = final;
	}

	public new int? FinalAssessment { get; private set; }

	private sealed record CreateFinalAssessmentRequest(int GradeId, int SubjectId, int StudentId);

	internal static GradeOfStudent Create(
		ApiClient client,
		int studentId,
		int subjectId,
		GetAssessmentsByIdResponse response,
		int periodId = 0
	)
	{
		return new GradeOfStudent(
			client: client,
			apiMethod: AssessmentControllerMethods.GetAssessmentsById(studentId: studentId),
			subjectId: subjectId,
			studentId: studentId,
			periodId: periodId,
			estimations: new AsyncLazy<List<EstimationOfStudent>>(valueFactory: async () =>
			{
				List<EstimationOfStudent> list = new List<EstimationOfStudent>(
					collection: response.Assessments.Select(selector: a => EstimationOfStudent.Create(
						client: client,
						id: a.Id,
						assessment: a.Assessment,
						createdAt: a.CreatedAt,
						comment: a.Comment,
						description: a.Description,
						gradeType: a.GradeType
					)
				));
				list.Sort(comparison: (first, second) => 0 - first.CreatedAt.CompareTo(value: second.CreatedAt));
				return list;
			}),
			average: response.AverageAssessment,
			final: response.FinalAssessment
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

	internal new async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		GetFinalAssessmentByIdResponse response = await _client.GetAsync<GetFinalAssessmentByIdResponse>(
			apiMethod: e.ApiMethodForFinalAT(arg: _studentId)
		) ?? throw new InvalidOperationException();
		FinalAssessment = response.FinalAssessment;
		InvokeCreatedFinalAssessment(e: e);
	}

	internal new async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		GetAssessmentResponse response = await _client.GetAsync<GetAssessmentResponse>(
			apiMethod: e.ApiMethod
		) ?? throw new InvalidOperationException();

		if (response.PeriodId != _periodId && _periodId != 0)
			return;

		_average = response.AverageAssessment;
		List<EstimationOfStudent> estimations = await _estimations;
		estimations.Add(item: EstimationOfStudent.Create(
			client: _client,
			id: response.Assessment.Id,
			assessment: response.Assessment.Assessment,
			createdAt: response.Assessment.CreatedAt,
			comment: response.Assessment.Comment,
			description: response.Assessment.Description,
			gradeType: response.Assessment.GradeType
		));
		estimations.Sort(comparison: (first, second) => 0 - second.CreatedAt.CompareTo(value: first.CreatedAt));
		InvokeCreatedAssessment(e: e);
	}
}