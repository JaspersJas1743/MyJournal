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
	private readonly IFileService _fileService;
	private readonly Lazy<List<EducationPeriod>> _educationPeriods;
	private readonly Lazy<List<TaughtSubject>> _subjects;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private TaughtSubjectCollection(
		ApiClient client,
		IFileService fileService,
		IEnumerable<TaughtSubject> studyingSubjects,
		IEnumerable<EducationPeriod> educationPeriods
	)
	{
		_client = client;
		_fileService = fileService;
		List<TaughtSubject> subjects = new List<TaughtSubject>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: TaughtSubject.Create(
			client: client,
			name: "Все классы",
			fileService: fileService
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<TaughtSubject>>(value: subjects);
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

	public TaughtSubject this[int index]
		=> _subjects.Value.ElementAtOrDefault(index: index)
		   ?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));
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
		IEnumerable<TaughtSubject.TaughtSubjectResponse> subjects = await LoadSubjectCollection(client: client, apiMethod: LessonControllerMethods.GetTaughtSubjects, cancellationToken: cancellationToken);
		IEnumerable<EducationPeriod> educationPeriods = await client.GetAsync<IEnumerable<EducationPeriod>>(
			apiMethod: TeacherControllerMethods.GetEducationPeriods,
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

		IEnumerable<TaughtSubject.TaughtSubjectResponse> response = await LoadSubjectCollection(
			client: _client,
			apiMethod: period.Id == 0 ? LessonControllerMethods.GetTaughtSubjects : LessonControllerMethods.GetTaughtSubjectsByPeriod(period: period.Name),
			cancellationToken: cancellationToken
		);
		List<TaughtSubject> subjects = new List<TaughtSubject>(collection: response.Select(selector: s => TaughtSubject.CreateWithoutTasks(
			client: _client,
			fileService: _fileService,
			response: s
		)));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: TaughtSubject.CreateWithoutTasks(client: _client, name: "Все классы", fileService: _fileService));

		_subjects.Value.Clear();
		_subjects.Value.AddRange(collection: subjects);
		_currentPeriod = period;
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		foreach (TaughtSubject subject in this.Where(predicate: ts => ts.Tasks.Any(predicate: t => t.Id == e.TaskId)))
			if (subject.Tasks.IsLoaded)
				await subject.OnCompletedTask(e: new TaughtSubject.CompletedTaskEventArgs(taskId: e.TaskId));

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		foreach (TaughtSubject subject in this.Where(predicate: ts => ts.Tasks.Any(predicate: t => t.Id == e.TaskId)))
			if (subject.Tasks.IsLoaded)
				await subject.OnUncompletedTask(e: new TaughtSubject.UncompletedTaskEventArgs(taskId: e.TaskId));

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		foreach (TaughtSubject subject in this.Where(predicate: ts => ts.Id == 0 || ts.Id == e.SubjectId))
			if (subject.Tasks.IsLoaded)
				await subject.OnCreatedTask(e: new TaughtSubject.CreatedTaskEventArgs(taskId: e.TaskId));

		CreatedTask?.Invoke(e: e);
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