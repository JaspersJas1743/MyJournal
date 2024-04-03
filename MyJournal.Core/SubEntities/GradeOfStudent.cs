using System.Diagnostics;
using MyJournal.Core.Collections;
using MyJournal.Core.EstimationBuilder;
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
		int subjectId,
		int studentId,
		AsyncLazy<List<EstimationOfStudent>> estimations,
		string average,
		string? final = null
	) : base(client: client, estimations: estimations, average: average, final: final)
	{
		_subjectId = subjectId;
		_studentId = studentId;
	}

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

	public IEstimationBuilder Add()
		=> EstimationBuilder.EstimationBuilder.Create(client: _client, studentId: _studentId, subjectId: _subjectId);

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