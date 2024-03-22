using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.Collections;

public class StudyingSubjectCollection : IEnumerable<StudyingSubject>
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
		_subjects = new Lazy<List<StudyingSubject>>(value: new List<StudyingSubject>(collection: studyingSubjects));
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
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<StudyingSubject> subjects = await client.GetAsync<IEnumerable<StudyingSubject>>(
			apiMethod: apiMethod,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new StudyingSubjectCollection(
			client: client,
			studyingSubjects: subjects
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