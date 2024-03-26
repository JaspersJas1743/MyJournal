using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

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
		IFileService fileService,
		IEnumerable<TaughtSubject> studyingSubjects
	)
	{
		_client = client;
		List<TaughtSubject> subjects = new List<TaughtSubject>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: TaughtSubject.Create(
			client: client,
			name: "Все дисциплины",
			fileService: fileService
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<TaughtSubject>>(value: subjects);
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
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<TaughtSubject.TaughtSubjectResponse> subjects = await client.GetAsync<IEnumerable<TaughtSubject.TaughtSubjectResponse>>(
			apiMethod: LessonControllerMethods.GetTaughtSubjects,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new TaughtSubjectCollection(
			client: client,
			fileService: fileService,
			studyingSubjects: subjects.Select(selector: s => TaughtSubject.Create(
				client: client,
				fileService: fileService,
				response: s,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
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