using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public class StudyingSubjectInClassCollection : IEnumerable<StudyingSubjectInClass>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<EducationPeriod>> _educationPeriods;
	private readonly Lazy<List<StudyingSubjectInClass>> _subjects;
	private readonly int _classId;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private StudyingSubjectInClassCollection(
		ApiClient client,
		int classId,
		IEnumerable<StudyingSubjectInClass> studyingSubjects,
		IEnumerable<EducationPeriod> educationPeriods
	)
	{
		_client = client;
		_classId = classId;
		List<StudyingSubjectInClass> subjects = new List<StudyingSubjectInClass>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: StudyingSubjectInClass.Create(
			client: client,
			classId: classId,
			name: "Все дисциплины"
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<StudyingSubjectInClass>>(value: subjects);
		List<EducationPeriod> periods = new List<EducationPeriod>(collection: educationPeriods);
		EducationPeriod currentTime = new EducationPeriod() { Id = 0, Name = periods.Count == 2 ? "Текущий семестр" : "Текущая четверть" };
		periods.Insert(index: 0, item: currentTime);
		_educationPeriods = new Lazy<List<EducationPeriod>>(value: periods);
		_currentPeriod = currentTime;
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

	public IEnumerable<EducationPeriod> EducationPeriods => _educationPeriods.Value;

	public StudyingSubjectInClass this[int index]
		=> _subjects.Value.ElementAtOrDefault(index: index)
		   ?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));
	#endregion

	#region Methods
	#region Static
	private static async Task<IEnumerable<StudyingSubjectInClass.StudyingSubjectResponse>> LoadSubjectCollection(
		ApiClient client,
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<StudyingSubjectInClass.StudyingSubjectResponse>>(
			apiMethod: apiMethod,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	public static async Task<StudyingSubjectInClassCollection> Create(
		ApiClient client,
		int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<StudyingSubjectInClass.StudyingSubjectResponse> subjects = await LoadSubjectCollection(
			client: client,
			apiMethod: LessonControllerMethods.GetSubjectsStudiedInClass(classId: classId),
			cancellationToken: cancellationToken
		);
		IEnumerable<EducationPeriod> educationPeriods = await client.GetAsync<IEnumerable<EducationPeriod>>(
			apiMethod: AdministratorControllerMethods.GetEducationPeriods(classId: classId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new StudyingSubjectInClassCollection(
			client: client,
			classId: classId,
			studyingSubjects: subjects.Select(selector: s => StudyingSubjectInClass.Create(
				client: client,
				classId: classId,
				response: s,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()),
			educationPeriods: educationPeriods
		);
	}
	#endregion

	#region Instance
	public async Task SetEducationPeriod(
		EducationPeriod period,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (_currentPeriod == period)
			return;

		IEnumerable<StudyingSubjectInClass.StudyingSubjectResponse> response = await LoadSubjectCollection(
			client: _client,
			apiMethod: period.Id == 0 ? LessonControllerMethods.GetSubjectsStudiedInClass(classId: _classId)
				: LessonControllerMethods.GetSubjectsStudiedInClassByPeriod(classId: _classId, period: period.Name),
			cancellationToken: cancellationToken
		);
		List<StudyingSubjectInClass> subjects = new List<StudyingSubjectInClass>(collection: response.Select(selector: s => StudyingSubjectInClass.CreateWithoutTasks(
			client: _client,
			response: s
		)));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: StudyingSubjectInClass.CreateWithoutTasks(client: _client, name: "Все дисциплины"));

		_subjects.Value.Clear();
		_subjects.Value.AddRange(collection: subjects);
		_currentPeriod = period;
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