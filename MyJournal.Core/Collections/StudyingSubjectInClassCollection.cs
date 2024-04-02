using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public class StudyingSubjectInClassCollection : IAsyncEnumerable<StudyingSubjectInClass>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<List<EducationPeriod>> _educationPeriods;
	private readonly AsyncLazy<List<StudyingSubjectInClass>> _subjects;
	private readonly int _classId;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private StudyingSubjectInClassCollection(
		ApiClient client,
		int classId,
		AsyncLazy<List<StudyingSubjectInClass>> studyingSubjects,
		AsyncLazy<List<EducationPeriod>> educationPeriods,
		EducationPeriod currentPeriod
	)
	{
		_client = client;
		_classId = classId;
		_subjects = studyingSubjects;
		_educationPeriods = educationPeriods;
		_currentPeriod = currentPeriod;
	}
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
		EducationPeriod currentPeriod = new EducationPeriod()
		{
			Id = 0,
			Name = educationPeriods.Count() == 2 ? "Текущий семестр" : "Текущая четверть"
		};
		return new StudyingSubjectInClassCollection(
			client: client,
			classId: classId,
			studyingSubjects: new AsyncLazy<List<StudyingSubjectInClass>>(valueFactory: async () =>
			{
				List<StudyingSubjectInClass> collection = new List<StudyingSubjectInClass>(collection: await Task.WhenAll(
					tasks: subjects.Select(selector: async s => await StudyingSubjectInClass.Create(
						client: client,
						classId: classId,
						response: s,
						cancellationToken: cancellationToken
					))
				));
				collection.Insert(index: 0, item: await StudyingSubjectInClass.Create(
					client: client,
					classId: classId,
					name: "Все дисциплины",
					cancellationToken: cancellationToken
				));
				return collection;
			}),
			educationPeriods: new AsyncLazy<List<EducationPeriod>>(valueFactory: async () =>
			{
				List<EducationPeriod> collection = new List<EducationPeriod>(collection: educationPeriods);
				collection.Insert(index: 0, item: currentPeriod);
				return collection;
			}),
			currentPeriod: currentPeriod
		);
	}
	#endregion

	#region Instance
	public async Task<int> GetLength()
	{
		List<StudyingSubjectInClass> collection = await _subjects;
		return collection.Count;
	}

	public async Task<IEnumerable<EducationPeriod>> GetEducationPeriods()
		=> await _educationPeriods;

	public async Task<StudyingSubjectInClass> GetByIndex(int index)
	{
		List<StudyingSubjectInClass> collection = await _subjects;
		return collection.ElementAtOrDefault(index: index) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index)
		);
	}

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
		List<StudyingSubjectInClass> subjects = new List<StudyingSubjectInClass>(collection: await Task.WhenAll(tasks: response.Select(
			selector: async subject => await StudyingSubjectInClass.CreateWithoutTasks(
				client: _client,
				response: subject,
				classId: _classId,
				educationPeriodId: period.Id,
				cancellationToken: cancellationToken
			)
		)));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: StudyingSubjectInClass.CreateWithoutTasks(name: "Все дисциплины"));

		List<StudyingSubjectInClass> collection = await _subjects;
		collection.Clear();
		collection.AddRange(collection: subjects);
		_currentPeriod = period;
	}
	#endregion

	#region IAsyncEnumerable<StudyingSubjectInClass>
	public async IAsyncEnumerator<StudyingSubjectInClass> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (StudyingSubjectInClass subject in await _subjects)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return subject;
		}
	}
	#endregion
	#endregion
}