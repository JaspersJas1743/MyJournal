using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class StudyingSubjectCollection : IEnumerable<StudyingSubject>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<StudyingSubject>> _subjects;
	#endregion

	#region Constructor
	private StudyingSubjectCollection(
		ApiClient client,
		IEnumerable<StudyingSubject> studyingSubjects
	)
	{
		_client = client;
		List<StudyingSubject> subjects = new List<StudyingSubject>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: StudyingSubject.Create(
			client: client,
			name: "Все дисциплины"
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<StudyingSubject>>(value: subjects);
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

	public StudyingSubject this[int index]
		=> _subjects.Value.ElementAtOrDefault(index: index)
		   ?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));
	#endregion

	#region Methods
	#region Instance
	public static async Task<StudyingSubjectCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<StudyingSubject.StudyingSubjectResponse> subjects = await client.GetAsync<IEnumerable<StudyingSubject.StudyingSubjectResponse>>(
			apiMethod: LessonControllerMethods.GetStudyingSubjects,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new StudyingSubjectCollection(
			client: client,
			studyingSubjects: subjects.Select(selector: s =>
				StudyingSubject.Create(
					client: client,
					response: s,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			)
		);
	}
	#endregion

	#region IEnumerable<StudyingSubject>
	public IEnumerator<StudyingSubject> GetEnumerator()
		=> _subjects.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion
	#endregion
}