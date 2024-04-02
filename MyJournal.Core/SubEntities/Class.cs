using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public class Class : ISubEntity, IAsyncEnumerable<StudentInClass>
{
	#region Fields
	private readonly AsyncLazy<StudyingSubjectInClassCollection> _studyingSubjects;
	private readonly AsyncLazy<List<StudentInClass>> _students;
	#endregion

	#region Constructors
	private Class(
		int id,
		string name,
		AsyncLazy<StudyingSubjectInClassCollection> studyingSubjects,
		AsyncLazy<List<StudentInClass>> students
	)
	{
		Id = id;
		Name = name;
		_studyingSubjects = studyingSubjects;
		_students = students;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	internal bool StudyingSubjectsAreCreated => _studyingSubjects.IsValueCreated;
	#endregion

	#region Records
	private sealed record GetStudentsFromClassResponse(int Id, string Surname, string Name, string? Patronymic);
	#endregion

	#region Methods
	#region Static
		internal static async Task<Class> Create(
		ApiClient client,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new Class(
			id: classId,
			name: name,
			studyingSubjects: new AsyncLazy<StudyingSubjectInClassCollection>(valueFactory: async () => await StudyingSubjectInClassCollection.Create(
				client: client,
				classId: classId,
				cancellationToken: cancellationToken
			)),
			students: new AsyncLazy<List<StudentInClass>>(valueFactory: async () =>
			{
				IEnumerable<GetStudentsFromClassResponse> students = await client.GetAsync<IEnumerable<GetStudentsFromClassResponse>>(
					apiMethod: ClassControllerMethods.GetStudentsFromClass(classId: classId),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return new List<StudentInClass>(collection: await Task.WhenAll(tasks: students.Select(
					selector: async s => await StudentInClass.Create(
						client: client,
						id: s.Id,
						surname: s.Surname,
						name: s.Name,
						patronymic: s.Patronymic,
						cancellationToken: cancellationToken
					)
				)));
			})
		);
	}
	#endregion

	#region Instance
	public async Task<StudyingSubjectInClassCollection> GetStudyingSubjects()
		=> await _studyingSubjects;

	public async Task<IEnumerable<StudentInClass>> GetStudents()
		=> await _students;
	#endregion

	#region IAsyncEnumerable<StudentInClass>
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
	#endregion
	#endregion
}