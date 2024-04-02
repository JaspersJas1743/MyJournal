using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity, IAsyncEnumerable<StudentInTaughtClass>
{
	private readonly ApiClient _client;
	private readonly AsyncLazy<IEnumerable<StudentInTaughtClass>> _students;

	private TaughtClass(
		ApiClient client,
		int subjectId,
		int id,
		string name,
		AsyncLazy<IEnumerable<StudentInTaughtClass>> students
	)
	{
		_client = client;
		_students = students;
		Id = id;
		Name = name;
		SubjectId = subjectId;
	}

	public int Id { get; init; }
	public string Name { get; init; }
	private int SubjectId { get; }

	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
	public sealed record Attendance(int StudentId, bool IsPresent, int? CommentId);
	private sealed record SetAttendanceRequest(int SubjectId, DateTime Datetime, IEnumerable<Attendance> Attendances);


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
			)))
		);
	}

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
}