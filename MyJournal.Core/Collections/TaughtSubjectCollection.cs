using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public class TaughtSubjectCollection : IAsyncEnumerable<TaughtSubject>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly AsyncLazy<List<EducationPeriod>> _educationPeriods;
	private readonly AsyncLazy<List<TaughtSubject>> _subjects;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private TaughtSubjectCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<TaughtSubject>> studyingSubjects,
		AsyncLazy<List<EducationPeriod>> educationPeriods,
		EducationPeriod currentPeriod
	)
	{
		_client = client;
		_fileService = fileService;
		_subjects = studyingSubjects;
		_educationPeriods = educationPeriods;
		_currentPeriod = currentPeriod;
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
	#region Static
	private static async Task<IEnumerable<TaughtSubject.TaughtSubjectResponse>> LoadSubjectCollection(
		ApiClient client,
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<TaughtSubject.TaughtSubjectResponse>>(
			apiMethod: apiMethod,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	public static async Task<TaughtSubjectCollection> Create(
		ApiClient client,
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<TaughtSubject.TaughtSubjectResponse> subjects = await LoadSubjectCollection(
			client: client,
			apiMethod: LessonControllerMethods.GetTaughtSubjects,
			cancellationToken: cancellationToken
		);
		IEnumerable<EducationPeriod> educationPeriods = await client.GetAsync<IEnumerable<EducationPeriod>>(
			apiMethod: TeacherControllerMethods.GetEducationPeriods,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		EducationPeriod currentPeriod = new EducationPeriod()
		{
			Id = 0,
			Name = educationPeriods.Count() == 2 ? "Текущий семестр" : "Текущая четверть"
		};
		return new TaughtSubjectCollection(
			client: client,
			fileService: fileService,
			studyingSubjects: new AsyncLazy<List<TaughtSubject>>(valueFactory: async () =>
			{
				List<TaughtSubject> collection = new List<TaughtSubject>(collection: await Task.WhenAll(
					tasks: subjects.Select(selector: async s => await TaughtSubject.Create(
						client: client,
						fileService: fileService,
						response: s,
						cancellationToken: cancellationToken
					))
				));
				collection.Insert(index: 0, item: await TaughtSubject.Create(
					client: client,
					name: "Все классы",
					fileService: fileService
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
		List<TaughtSubject> collection = await _subjects;
		return collection.Count;
	}

	public async Task<IEnumerable<EducationPeriod>> GetEducationPeriods()
	{
		List<EducationPeriod> collection = await _educationPeriods;
		return collection;
	}

	public async Task<TaughtSubject> GetByIndex(int index)
	{
		List<TaughtSubject> collection = await _subjects;
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

		IEnumerable<TaughtSubject.TaughtSubjectResponse> response = await LoadSubjectCollection(
			client: _client,
			apiMethod: period.Id == 0 ? LessonControllerMethods.GetTaughtSubjects : LessonControllerMethods.GetTaughtSubjectsByPeriod(period: period.Name),
			cancellationToken: cancellationToken
		);
		List<TaughtSubject> subjects = new List<TaughtSubject>(collection: response.Select(selector: s => TaughtSubject.CreateWithoutTasks(
			fileService: _fileService,
			response: s
		)));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: TaughtSubject.CreateWithoutTasks(name: "Все классы", fileService: _fileService));

		List<TaughtSubject> collection = await _subjects;
		collection.Clear();
		collection.AddRange(collection: subjects);
		_currentPeriod = period;
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(invocation: async subject =>
		{
			CreatedTaskCollection tasks = await subject.GetTasks();
			if (await tasks.AnyAsync(predicate: task => task.Id == e.TaskId))
				await subject.OnCompletedTask(e: new TaughtSubject.CompletedTaskEventArgs(taskId: e.TaskId));
		}, filter: _ => true);

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(invocation: async subject =>
		{
			CreatedTaskCollection tasks = await subject.GetTasks();
			if (await tasks.AnyAsync(predicate: task => task.Id == e.TaskId))
				await subject.OnUncompletedTask(e: new TaughtSubject.UncompletedTaskEventArgs(taskId: e.TaskId));
		}, filter: _ => true);

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCompletedTask(e: new TaughtSubject.CompletedTaskEventArgs(taskId: e.TaskId)),
			filter: subject => subject.Id == 0 || subject.Id == e.SubjectId
		);

		CreatedTask?.Invoke(e: e);
	}

	private async Task InvokeIfSubjectsAreCreated(Func<TaughtSubject, Task> invocation, Predicate<TaughtSubject> filter)
	{
		if (!_subjects.IsValueCreated)
			return;

		List<TaughtSubject> collection = await _subjects;
		foreach (TaughtSubject subject in collection.FindAll(match: subject => subject.TasksAreCreated && filter(obj: subject)))
			await invocation(arg: subject);
	}
	#endregion

	#region IAsyncEnumerable<TaughtSubject>
	public async IAsyncEnumerator<TaughtSubject> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (TaughtSubject subject in await _subjects)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return subject;
		}
	}
	#endregion
	#endregion
}