using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class StudyingSubjectCollection : IAsyncEnumerable<StudyingSubject>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly AsyncLazy<List<StudyingSubject>> _subjects;
	private readonly AsyncLazy<List<EducationPeriod>> _educationPeriods;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private StudyingSubjectCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<StudyingSubject>> studyingSubjects,
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

	#region Events
	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;
	public event CreatedTaskHandler CreatedTask;
	public event CreatedFinalAssessmentHandler CreatedFinalAssessment;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
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
		EducationPeriod currentPeriod = new EducationPeriod()
		{
			Id = 0,
			Name = educationPeriods.Count() == 2 ? "Текущий семестр" : "Текущая четверть"
		};
		return new StudyingSubjectCollection(
			client: client,
			fileService: fileService,
			studyingSubjects: new AsyncLazy<List<StudyingSubject>>(valueFactory: async () =>
			{
				List<StudyingSubject> collection = new List<StudyingSubject>(collection: await Task.WhenAll(tasks: subjects.Select(
					selector: async s => await StudyingSubject.Create(
						client: client,
						fileService: fileService,
						response: s,
						cancellationToken: cancellationToken
					)
				)));
				collection.Insert(index: 0, item: await StudyingSubject.Create(client: client, fileService: fileService, name: "Все дисциплины", cancellationToken: cancellationToken));
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
		List<StudyingSubject> collection = await _subjects;
		return collection.Count;
	}

	public async Task<IEnumerable<EducationPeriod>> GetEducationPeriods()
		=> await _educationPeriods;

	public async Task<StudyingSubject> GetById(int id)
	{
		List<StudyingSubject> collection = await _subjects;
		return collection.Find(match: s => s.Id == id) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с идентификатором {id} отсутствует.", paramName: nameof(id)
		);
	}

	public async Task<StudyingSubject> GetByIndex(int index)
	{
		List<StudyingSubject> collection = await _subjects;
		return collection.ElementAtOrDefault(index: index) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index)
		);
	}

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
		List<StudyingSubject> subjects = new List<StudyingSubject>(collection: await Task.WhenAll(tasks: response.Select(
			selector: async s => await StudyingSubject.CreateWithoutTasks(
				client: _client,
				fileService: _fileService,
				response: s,
				periodId: period.Id,
				cancellationToken: cancellationToken
			)
		)));

		if (period.Id == 0)
			subjects.Insert(index: 0, item: StudyingSubject.CreateWithoutTasks(name: "Все дисциплины"));

		List<StudyingSubject> collection = await _subjects;
		collection.Clear();
		collection.AddRange(collection: subjects);
		_currentPeriod = period;
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(invocation: async subject =>
		{
			AssignedTaskCollection tasks = await subject.GetTasks();
			if (await tasks.AnyAsync(predicate: task => task.Id == e.TaskId))
				await subject.OnCompletedTask(e: e);
		}, filter: subject => subject.TasksAreCreated);

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(invocation: async subject =>
		{
			AssignedTaskCollection tasks = await subject.GetTasks();
			if (await tasks.AnyAsync(predicate: task => task.Id == e.TaskId))
				await subject.OnUncompletedTask(e: e);
		}, filter: subject => subject.TasksAreCreated);

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedTask(e: e),
			filter: subject => subject.TasksAreCreated && (subject.Id == 0 || subject.Id == e.SubjectId)
		);

		CreatedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedFinalAssessment(e: e),
			filter: subject => subject.Id == e.SubjectId && subject.GradeIsCreated
		);

		CreatedFinalAssessment?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedAssessment(e: e),
			filter: subject => subject.Id == e.SubjectId && subject.GradeIsCreated
		);

		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnChangedAssessment(e: e),
			filter: subject => subject.Id == e.SubjectId && subject.GradeIsCreated
		);

		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnDeletedAssessment(e: e),
			filter: subject => subject.Id == e.SubjectId && subject.GradeIsCreated
		);

		DeletedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedTimetable(ChangedTimetableEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnChangedTimetable(e: e),
			filter: _ => true
		);
	}

	private async Task InvokeIfSubjectsAreCreated(
		Func<StudyingSubject, Task> invocation,
		Predicate<StudyingSubject> filter
	)
	{
		if (!_subjects.IsValueCreated)
			return;

		List<StudyingSubject> collection = await _subjects;
		foreach (StudyingSubject subject in collection.FindAll(match: filter))
			await invocation(arg: subject);
	}
	#endregion

	#region IAsyncEnumerable<StudyingSubject>
	public async IAsyncEnumerator<StudyingSubject> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (StudyingSubject subject in await _subjects)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return subject;
		}
	}
	#endregion
	#endregion
}