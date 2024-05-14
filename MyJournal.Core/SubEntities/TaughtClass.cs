using System.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity, IAsyncEnumerable<StudentInTaughtClass>
{
	private readonly ApiClient _client;
	private readonly AsyncLazy<IEnumerable<StudentInTaughtClass>> _students;
	private readonly AsyncLazy<IEnumerable<CommentsForAssessment>> _commentsForTruancy;

	public static readonly TaughtClass Empty = new TaughtClass();

	private TaughtClass() { }

	private TaughtClass(
		ApiClient client,
		int subjectId,
		int id,
		string name,
		AsyncLazy<IEnumerable<StudentInTaughtClass>> students,
		AsyncLazy<IEnumerable<CommentsForAssessment>> commentsForTruancy
	)
	{
		_client = client;
		_students = students;
		Id = id;
		Name = name;
		SubjectId = subjectId;
		_commentsForTruancy = commentsForTruancy;
	}

	public int Id { get; init; }
	public string Name { get; init; }
	private int SubjectId { get; }
	internal bool StudentsAreCreated => _students.IsValueCreated;

	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
	public sealed record Attendance(int StudentId, bool IsPresent, int? CommentId);
	private sealed record SetAttendanceRequest(int SubjectId, DateTime Datetime, IEnumerable<Attendance> Attendances);

	#region Events
	internal event CreatedAssessmentHandler CreatedAssessment;
	internal event ChangedAssessmentHandler ChangedAssessment;
	internal event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	public async Task<IEnumerable<StudentInTaughtClass>> GetStudents()
		=> await _students;

	internal static async Task<TaughtClass> Create(
		ApiClient client,
		int subjectId,
		int classId,
		string name,
		int educationPeriodId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetStudentsFromClassResponse> students = await client.GetAsync<IEnumerable<GetStudentsFromClassResponse>>(
			apiMethod: ClassControllerMethods.GetStudentsFromClass(classId: classId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaughtClass(
			client: client,
			id: classId,
			name: name,
			subjectId: subjectId,
			students: new AsyncLazy<IEnumerable<StudentInTaughtClass>>(valueFactory: async () => await Task.WhenAll(tasks: students.Select(
				selector: async s => await StudentInTaughtClass.Create(
					client: client,
					subjectId: subjectId,
					id: s.Id,
					surname: s.Surname,
					name: s.Name,
					patronymic: s.Patronymic,
					educationPeriodId: educationPeriodId,
					cancellationToken: cancellationToken
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

	public async Task<IEnumerable<CommentsForAssessment>> GetCommentsForTruancy()
		=> await _commentsForTruancy;

	public async Task SetAttendance(
		DateTime date,
		IEnumerable<Attendance> attendance,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PostAsync<SetAttendanceRequest>(
			apiMethod: AssessmentControllerMethods.SetAttendance,
			arg: new SetAttendanceRequest(SubjectId: SubjectId, Datetime: date, Attendances: attendance),
			cancellationToken: cancellationToken
		);
	}

	public async IAsyncEnumerator<StudentInTaughtClass> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (StudentInTaughtClass student in await _students)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return student;
		}
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
		StudentInTaughtClass? student = await this.SingleOrDefaultAsync(predicate: s => s.Id == studentId);
		if (student is not null && student.GradeIsCreated)
			await invocation(arg: student);
	}
}