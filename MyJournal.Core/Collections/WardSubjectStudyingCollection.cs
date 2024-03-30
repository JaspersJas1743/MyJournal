using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class WardSubjectStudyingCollection : IEnumerable<WardSubjectStudying>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<List<EducationPeriod>> _educationPeriods;
	private readonly AsyncLazy<List<WardSubjectStudying>> _subjects;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private WardSubjectStudyingCollection(
		ApiClient client,
		AsyncLazy<List<WardSubjectStudying>> studyingSubjects,
		AsyncLazy<List<EducationPeriod>> educationPeriods,
		EducationPeriod currentPeriod
	)
	{
		_client = client;
		// List<WardSubjectStudying> subjects = new List<WardSubjectStudying>(collection: studyingSubjects);
		// subjects.Insert(index: 0, item: WardSubjectStudying.Create(
		// 	client: client,
		// 	name: "Все дисциплины"
		// ).GetAwaiter().GetResult());
		// _subjects = new Lazy<List<WardSubjectStudying>>(value: subjects);
		_subjects = studyingSubjects;
		// List<EducationPeriod> periods = new List<EducationPeriod>(collection: educationPeriods);
		// EducationPeriod currentTime = new EducationPeriod() { Id = 0, Name = periods.Count == 2 ? "Текущий семестр" : "Текущая четверть" };
		// periods.Insert(index: 0, item: currentTime);
		// _educationPeriods = new Lazy<List<EducationPeriod>>(value: periods);
		_educationPeriods = educationPeriods;
		_currentPeriod = currentPeriod;
	}
	#endregion

	#region Properties
	public async Task<int> GetLength()
	{
		List<WardSubjectStudying> collection = await _subjects;
		return collection.Count;
	}

	public async Task<IEnumerable<EducationPeriod>> GetEducationPeriods()
		=> await _educationPeriods;

	public async Task<WardSubjectStudying> GetByIndex(int index)
	{
		List<WardSubjectStudying> collection = await _subjects;
		return collection.ElementAtOrDefault(index: index) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index)
		);
	}
	#endregion

	#region Classes
	public sealed class CompletedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	public sealed class UncompletedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	public sealed class CreatedTaskEventArgs(int taskId, int subjectId) : EventArgs
	{
		public int TaskId { get; } = taskId;
		public int SubjectId { get; } = subjectId;
	}
	#endregion

	#region Delegates
	public delegate void CompletedTaskHandler(CompletedTaskEventArgs e);
	public delegate void UncompletedTaskHandler(UncompletedTaskEventArgs e);
	public delegate void CreatedTaskHandler(CreatedTaskEventArgs e);
	#endregion

	#region Events
	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;
	public event CreatedTaskHandler CreatedTask;
	#endregion

	#region Methods
	#region Instance
	private static async Task<IEnumerable<WardSubjectStudying.StudyingSubjectResponse>> LoadSubjectCollection(
		ApiClient client,
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<WardSubjectStudying.StudyingSubjectResponse>>(
			apiMethod: apiMethod,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	public static async Task<WardSubjectStudyingCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<WardSubjectStudying.StudyingSubjectResponse> subjects = await LoadSubjectCollection(
			client: client,
			apiMethod: LessonControllerMethods.GetSubjectsStudiedByWard,
			cancellationToken: cancellationToken
		);
		IEnumerable<EducationPeriod> educationPeriods = await client.GetAsync<IEnumerable<EducationPeriod>>(
			apiMethod: ParentControllerMethods.GetEducationPeriods,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		EducationPeriod currentPeroid = new EducationPeriod()
		{
			Id = 0,
			Name = educationPeriods.Count() == 2 ? "Текущий семестр" : "Текущая четверть"
		};
		return new WardSubjectStudyingCollection(
			client: client,
			studyingSubjects: new AsyncLazy<List<WardSubjectStudying>>(valueFactory: async () =>
			{
				List<WardSubjectStudying> collection = new List<WardSubjectStudying>(collection: await Task.WhenAll(tasks: subjects.Select(
					selector: async s => await WardSubjectStudying.Create(
						client: client,
						response: s,
						cancellationToken: cancellationToken
					)
				)));
				collection.Insert(index: 0, item: await WardSubjectStudying.Create(
					client: client,
					name: "Все дисциплины",
					cancellationToken: cancellationToken
				));
				return collection;
			}),
			educationPeriods: new AsyncLazy<List<EducationPeriod>>(valueFactory: async () =>
			{
				List<EducationPeriod> collection = new List<EducationPeriod>(collection: educationPeriods);
				collection.Insert(index: 0, item: currentPeroid);
				return collection;
			}),
			currentPeriod: currentPeroid
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

		IEnumerable<WardSubjectStudying.StudyingSubjectResponse> response = await LoadSubjectCollection(
			client: _client,
			apiMethod: period.Id == 0 ? LessonControllerMethods.GetSubjectsStudiedByWard
				: LessonControllerMethods.GetSubjectsStudiedByWardByPeriod(period: period.Name),
			cancellationToken: cancellationToken
		);
		List<WardSubjectStudying> subjects = new List<WardSubjectStudying>(collection: response.Select(
			selector: s => WardSubjectStudying.CreateWithoutTasks(client: _client, response: s)
		));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: WardSubjectStudying.CreateWithoutTasks(client: _client, name: "Все дисциплины"));

		List<WardSubjectStudying> collection = await _subjects;
		collection.Clear();
		collection.AddRange(collection: subjects);
		_currentPeriod = period;
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		// foreach (WardSubjectStudying subject in this.Where(predicate: ts => ts.Tasks.Any(predicate: t => t.Id == e.TaskId)))
		// 	await subject.OnCompletedTask(e: new WardSubjectStudying.CompletedTaskEventArgs(taskId: e.TaskId));
			// if (subject.Tasks.IsLoaded)

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		// foreach (WardSubjectStudying subject in this.Where(predicate: ts => ts.Tasks.Any(predicate: t => t.Id == e.TaskId)))
		// 	await subject.OnUncompletedTask(e: new WardSubjectStudying.UncompletedTaskEventArgs(taskId: e.TaskId));
			// if (subject.Tasks.IsLoaded)

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		// foreach (WardSubjectStudying subject in this.Where(predicate: ts => ts.Id == 0 || ts.Id == e.SubjectId))
		// 	await subject.OnCreatedTask(e: new WardSubjectStudying.CreatedTaskEventArgs(taskId: e.TaskId));
			// if (subject.Tasks.IsLoaded)

		CreatedTask?.Invoke(e: e);
	}
	#endregion

	#region IEnumerable<WardSubjectStudying>
	public IEnumerator<WardSubjectStudying> GetEnumerator()
		=> _subjects.GetAwaiter().GetResult().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion
	#endregion
}