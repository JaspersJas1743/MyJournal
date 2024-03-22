using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public class TaughtSubjectCollection : IEnumerable<TaughtSubject>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<TaughtSubject>> _subjects;
	#endregion

	#region Constructor
	private TaughtSubjectCollection(
		ApiClient client,
		IEnumerable<TaughtSubject> studyingSubjects
	)
	{
		_client = client;
		_subjects = new Lazy<List<TaughtSubject>>(value: new List<TaughtSubject>(collection: studyingSubjects));
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

	public TaughtSubject this[int index]
		=> _subjects.Value.ElementAtOrDefault(index: index)
		   ?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));
	#endregion

	#region Methods
	#region Instance
	public static async Task<TaughtSubjectCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<TaughtSubject> subjects = await client.GetAsync<IEnumerable<TaughtSubject>>(
			apiMethod: LessonControllerMethods.GetTaughtSubjects,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaughtSubjectCollection(
			client: client,
			studyingSubjects: subjects
		);
	}
	#endregion

	#region IEnumerable<TaughtSubject>
	public IEnumerator<TaughtSubject> GetEnumerator()
		=> _subjects.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion
	#endregion
}