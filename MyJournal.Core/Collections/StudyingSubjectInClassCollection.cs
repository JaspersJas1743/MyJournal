using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public class StudyingSubjectInClassCollection : IEnumerable<StudyingSubjectInClass>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<StudyingSubjectInClass>> _subjects;
	#endregion

	#region Constructor
	private StudyingSubjectInClassCollection(
		ApiClient client,
		int classId,
		IEnumerable<StudyingSubjectInClass> studyingSubjects
	)
	{
		_client = client;
		List<StudyingSubjectInClass> subjects = new List<StudyingSubjectInClass>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: StudyingSubjectInClass.Create(
			client: client,
			classId: classId,
			name: "Все дисциплины"
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<StudyingSubjectInClass>>(value: subjects);
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

	public StudyingSubjectInClass this[int index]
		=> _subjects.Value.ElementAtOrDefault(index: index)
		   ?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));
	#endregion

	#region Methods
	#region Instance
	public static async Task<StudyingSubjectInClassCollection> Create(
		ApiClient client,
		int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<StudyingSubjectInClass.StudyingSubjectResponse> subjects = await client.GetAsync<IEnumerable<StudyingSubjectInClass.StudyingSubjectResponse>>(
			apiMethod: LessonControllerMethods.GetSubjectsStudiedInClass(classId: classId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new StudyingSubjectInClassCollection(
			client: client,
			classId: classId,
			studyingSubjects: subjects.Select(selector: s => StudyingSubjectInClass.Create(
				client: client,
				classId: classId,
				response: s
			).GetAwaiter().GetResult()
		));
	}
	#endregion

	#region IEnumerable<StudyingSubjectInClass>
	public IEnumerator<StudyingSubjectInClass> GetEnumerator()
		=> _subjects.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion
	#endregion
}