using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class StudyingSubjectCollection : IEnumerable<StudyingSubject>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly Lazy<List<StudyingSubject>> _subjects;
	private readonly Lazy<List<EducationPeriod>> _educationPeriods;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private StudyingSubjectCollection(
		ApiClient client,
		IFileService fileService,
		IEnumerable<StudyingSubject> studyingSubjects,
		IEnumerable<EducationPeriod> educationPeriods
	)
	{
		_client = client;
		_fileService = fileService;
		List<StudyingSubject> subjects = new List<StudyingSubject>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: StudyingSubject.Create(
			client: client,
			name: "Все дисциплины"
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<StudyingSubject>>(value: subjects);
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

	public StudyingSubject this[int index]
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
	private static async Task<IEnumerable<StudyingSubject.StudyingSubjectResponse>> LoadSubjectCollection(
		ApiClient client,
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<IEnumerable<StudyingSubject.StudyingSubjectResponse>>(
			apiMethod: apiMethod,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	public static async Task<StudyingSubjectCollection> Create(
		ApiClient client,
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<StudyingSubject.StudyingSubjectResponse> subjects = await LoadSubjectCollection(
			client: client,
			apiMethod: LessonControllerMethods.GetStudyingSubjects,
			cancellationToken: cancellationToken
		);
		IEnumerable<EducationPeriod> educationPeriods = await client.GetAsync<IEnumerable<EducationPeriod>>(
			apiMethod: StudentControllerMethods.GetEducationPeriods,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new StudyingSubjectCollection(
			client: client,
			fileService: fileService,
			studyingSubjects: subjects.Select(selector: s =>
				StudyingSubject.Create(
					client: client,
					response: s,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			),
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
		if (period == _currentPeriod)
			return;

		IEnumerable<StudyingSubject.StudyingSubjectResponse> response = await LoadSubjectCollection(
			client: _client,
			apiMethod: period.Id == 0 ? LessonControllerMethods.GetStudyingSubjects : LessonControllerMethods.GetStudyingSubjectsByPeriod(period: period.Name),
			cancellationToken: cancellationToken
		);
		List<StudyingSubject> subjects = new List<StudyingSubject>(collection: response.Select(selector: s => StudyingSubject.CreateWithoutTasks(
			client: _client,
			response: s
		)));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: StudyingSubject.CreateWithoutTasks(client: _client, name: "Все дисциплины"));

		_subjects.Value.Clear();
		_subjects.Value.AddRange(collection: subjects);
		_currentPeriod = period;
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		foreach (StudyingSubject subject in this.Where(predicate: ts => ts.Tasks.Any(predicate: t => t.Id == e.TaskId)))
			if (subject.Tasks.IsLoaded)
				await subject.OnCompletedTask(e: new StudyingSubject.CompletedTaskEventArgs(taskId: e.TaskId));

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		foreach (StudyingSubject subject in this.Where(predicate: ts => ts.Tasks.Any(predicate: t => t.Id == e.TaskId)))
			if (subject.Tasks.IsLoaded)
				await subject.OnUncompletedTask(e: new StudyingSubject.UncompletedTaskEventArgs(taskId: e.TaskId));

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		foreach (StudyingSubject subject in this.Where(predicate: ts => ts.Id == 0 || ts.Id == e.SubjectId))
			if (subject.Tasks.IsLoaded)
				await subject.OnCreatedTask(e: new StudyingSubject.CreatedTaskEventArgs(taskId: e.TaskId));

		CreatedTask?.Invoke(e: e);
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