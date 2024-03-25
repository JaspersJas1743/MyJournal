using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class WardSubjectStudyingCollection : IEnumerable<WardSubjectStudying>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<WardSubjectStudying>> _subjects;
	#endregion

	#region Constructor
	private WardSubjectStudyingCollection(
		ApiClient client,
		IEnumerable<WardSubjectStudying> studyingSubjects
	)
	{
		_client = client;
		List<WardSubjectStudying> subjects = new List<WardSubjectStudying>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: WardSubjectStudying.Create(
			client: client,
			name: "Все дисциплины"
		));
		_subjects = new Lazy<List<WardSubjectStudying>>(value: subjects);
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

	public WardSubjectStudying this[int index]
		=> _subjects.Value.ElementAtOrDefault(index: index)
		   ?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));
	#endregion

	#region Methods
	#region Instance
	public static async Task<WardSubjectStudyingCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<WardSubjectStudying.StudyingSubjectResponse> subjects = await client.GetAsync<IEnumerable<WardSubjectStudying.StudyingSubjectResponse>>(
			apiMethod: LessonControllerMethods.GetSubjectsStudiedByWard,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new WardSubjectStudyingCollection(
			client: client,
			studyingSubjects: subjects.Select(selector: s => WardSubjectStudying.Create(
				client: client,
				response: s
			))
		);
	}
	#endregion

	#region IEnumerable<WardSubjectStudying>
	public IEnumerator<WardSubjectStudying> GetEnumerator()
		=> _subjects.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion
	#endregion
}