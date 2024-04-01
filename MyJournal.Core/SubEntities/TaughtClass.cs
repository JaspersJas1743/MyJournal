using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity, IAsyncEnumerable<StudentInClass>
{
	private readonly ApiClient _client;
	private readonly AsyncLazy<IEnumerable<StudentInClass>> _students;

	private TaughtClass(
		ApiClient client,
		int subjectId,
		int id,
		string name,
		AsyncLazy<IEnumerable<StudentInClass>> students
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


	public async Task<IEnumerable<StudentInClass>> GetStudents()
		=> await _students;

	internal static async Task<TaughtClass> Create(
		ApiClient client,
		int subjectId,
		int id,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetStudentsFromClassResponse> students = await client.GetAsync<IEnumerable<GetStudentsFromClassResponse>>(
			apiMethod: ClassControllerMethods.GetStudentsFromClass(classId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaughtClass(
			client: client,
			id: id,
			name: name,
			subjectId: subjectId,
			students: new AsyncLazy<IEnumerable<StudentInClass>>(valueFactory: async () => await Task.WhenAll(tasks: students.Select(
				selector: async s => await StudentInClass.Create(
					client: client,
					subjectId: subjectId,
					id: s.Id,
					surname: s.Surname,
					name: s.Name,
					patronymic: s.Patronymic,
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

	public async IAsyncEnumerator<StudentInClass> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (StudentInClass student in await _students)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return student;
		}
	}
}