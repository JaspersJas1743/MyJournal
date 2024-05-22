using System.Collections;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity, IEnumerable<StudentInTaughtClass>
{
	private readonly AsyncLazy<IEnumerable<CommentsForAssessment>> _commentsForTruancy;
	private readonly ApiClient _client;

	public static readonly TaughtClass Empty = new TaughtClass();

	private TaughtClass() { }

	private TaughtClass(
		ApiClient client,
		int subjectId,
		int id,
		string name,
		IEnumerable<StudentInTaughtClass> students,
		AsyncLazy<IEnumerable<CommentsForAssessment>> commentsForTruancy
	)
	{
		_client = client;
		Students = students;
		Id = id;
		Name = name;
		SubjectId = subjectId;
		_commentsForTruancy = commentsForTruancy;
	}

	public int Id { get; init; }
	public string Name { get; init; }
	private int SubjectId { get; }

	private sealed record GetAssessmentsRequest(int PeriodId, int SubjectId);
	private sealed record Grade(int Id, string Assessment, DateTime CreatedAt, string? Comment, string Description, GradeTypes GradeType);
	private sealed record Student(int StudentId, string Surname, string Name, string? Patronymic);
	private sealed record GetAssessmentsByClassResponse(Student Student, string AverageAssessment, int? FinalAssessment, IEnumerable<Grade> Assessments);
	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
	public sealed record Attendance(int StudentId, bool IsPresent, int? CommentId);
	private sealed record SetAttendanceRequest(int SubjectId, DateTime Datetime, IEnumerable<Attendance> Attendances);

	#region Events
	internal event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	internal event CreatedAssessmentHandler CreatedAssessment;
	internal event ChangedAssessmentHandler ChangedAssessment;
	internal event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	public IEnumerable<StudentInTaughtClass> Students { get; private set; }

	internal static async Task<TaughtClass> Create(
		ApiClient client,
		int subjectId,
		int classId,
		string name,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetAssessmentsByClassResponse> students = await client.GetAsync<IEnumerable<GetAssessmentsByClassResponse>, GetAssessmentsRequest>(
			apiMethod: AssessmentControllerMethods.GetAssessmentsByClass(classId: classId),
			argQuery: new GetAssessmentsRequest(PeriodId: educationPeriodId, SubjectId: subjectId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaughtClass(
			client: client,
			id: classId,
			name: name,
			subjectId: subjectId,
			students: await Task.WhenAll(tasks: students.Select(selector: s => StudentInTaughtClass.Create(
				client: client,
				subjectId: subjectId,
				id: s.Student.StudentId,
				surname: s.Student.Surname,
				name: s.Student.Name,
				patronymic: s.Student.Patronymic,
				educationPeriodId: educationPeriodId,
				response: new GetAssessmentsByIdResponse(
					AverageAssessment: s.AverageAssessment,
					FinalAssessment: s.FinalAssessment,
					Assessments: s.Assessments.Select(
						selector: e => new EstimationResponse(
							Id: e.Id,
							Assessment: e.Assessment,
							CreatedAt: e.CreatedAt,
							Comment: e.Comment,
							Description: e.Description,
							GradeType: e.GradeType
						)
					)
				)
			))),
			commentsForTruancy: new AsyncLazy<IEnumerable<CommentsForAssessment>>(valueFactory: async () =>
			{
				IEnumerable<CommentsForAssessment> response = await client.GetAsync<IEnumerable<CommentsForAssessment>>(
					apiMethod: AssessmentControllerMethods.GetCommentsForTruancy,
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return response;
			})
		);
	}

	public async Task SetEducationPeriod(
		int educationPeriodId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetAssessmentsByClassResponse> students = await _client.GetAsync<IEnumerable<GetAssessmentsByClassResponse>, GetAssessmentsRequest>(
			apiMethod: AssessmentControllerMethods.GetAssessmentsByClass(classId: Id),
			argQuery: new GetAssessmentsRequest(PeriodId: educationPeriodId, SubjectId: SubjectId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Students = await Task.WhenAll(tasks: students.Select(selector: s => StudentInTaughtClass.Create(
			client: _client,
			subjectId: SubjectId,
			id: s.Student.StudentId,
			surname: s.Student.Surname,
			name: s.Student.Name,
			patronymic: s.Student.Patronymic,
			educationPeriodId: educationPeriodId,
			response: new GetAssessmentsByIdResponse(
				AverageAssessment: s.AverageAssessment,
				FinalAssessment: s.FinalAssessment,
				Assessments: s.Assessments.Select(
					selector: e => new EstimationResponse(
						Id: e.Id,
						Assessment: e.Assessment,
						CreatedAt: e.CreatedAt,
						Comment: e.Comment,
						Description: e.Description,
						GradeType: e.GradeType
					)
				)
			)
		)));
	}

	public async Task<IEnumerable<CommentsForAssessment>> GetCommentsForTruancy()
		=> await _commentsForTruancy;

	public async Task SetAttendance(
		DateTime date,
		IEnumerable<Attendance> attendance,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PutAsync<SetAttendanceRequest>(
			apiMethod: AssessmentControllerMethods.SetAttendance,
			arg: new SetAttendanceRequest(SubjectId: SubjectId, Datetime: date, Attendances: attendance),
			cancellationToken: cancellationToken
		);
	}

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(invocation: student => student.OnCreatedFinalAssessment(e: e), studentId: e.StudentId);
		CreatedFinalAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(invocation: student => student.OnCreatedAssessment(e: e), studentId: e.StudentId);
		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		await InvokeIfStudentIsCreated(invocation: student => student.OnChangedAssessment(e: e), studentId: e.StudentId);
		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
    {
		await InvokeIfStudentIsCreated(invocation: student => student.OnDeletedAssessment(e: e), studentId: e.StudentId);
		DeletedAssessment?.Invoke(e: e);
	}

	private async Task InvokeIfStudentIsCreated(Func<StudentInTaughtClass, Task> invocation, int studentId)
	{
		StudentInTaughtClass? student = this.SingleOrDefault(predicate: s => s.Id == studentId);
		if (student is not null)
			await invocation(arg: student);
	}

	public IEnumerator<StudentInTaughtClass> GetEnumerator()
		=> Students.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
}