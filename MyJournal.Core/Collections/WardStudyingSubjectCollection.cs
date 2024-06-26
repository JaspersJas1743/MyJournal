using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class WardStudyingSubjectCollection : IAsyncEnumerable<WardSubjectStudying>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<List<EducationPeriod>> _educationPeriods;
	private readonly AsyncLazy<List<WardSubjectStudying>> _subjects;

	private EducationPeriod _currentPeriod;
	#endregion

	#region Constructor
	private WardStudyingSubjectCollection(
		ApiClient client,
		AsyncLazy<List<WardSubjectStudying>> studyingSubjects,
		AsyncLazy<List<EducationPeriod>> educationPeriods,
		EducationPeriod currentPeriod
	)
	{
		_client = client;
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

	public static async Task<WardStudyingSubjectCollection> Create(
		ApiClient client,
		IFileService fileService,
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
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		EducationPeriod? educationPeriod = educationPeriods.FirstOrDefault(predicate: p => p.StartDate <= now && p.EndDate >= now);
		EducationPeriod currentPeriod = new EducationPeriod()
		{
			Id = 0,
			Name = educationPeriods.Count() == 2 ? "Текущий семестр" : "Текущая четверть",
			StartDate = educationPeriod?.StartDate,
			EndDate = educationPeriod?.EndDate
		};
		return new WardStudyingSubjectCollection(
			client: client,
			studyingSubjects: new AsyncLazy<List<WardSubjectStudying>>(valueFactory: async () =>
			{
				List<WardSubjectStudying> collection = new List<WardSubjectStudying>(collection: await Task.WhenAll(tasks: subjects.Select(
					selector: async s => await WardSubjectStudying.Create(
						client: client,
						fileService: fileService,
						response: s,
						cancellationToken: cancellationToken
					)
				)));
				if (collection.Count <= 0)
					return collection;

				collection.Insert(index: 0, item: await WardSubjectStudying.Create(
					client: client,
					fileService: fileService,
					name: "Все дисциплины",
					cancellationToken: cancellationToken
				));
				return collection;
			}),
			educationPeriods: new AsyncLazy<List<EducationPeriod>>(valueFactory: async () =>
			{
				List<EducationPeriod> collection = new List<EducationPeriod>(collection: educationPeriods);

				if (educationPeriod is not null)
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

	public async Task<WardSubjectStudying> GetById(int id)
	{
		List<WardSubjectStudying> collection = await _subjects;
		return collection.Find(match: s => s.Id == id) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с идентификатором {id} отсутствует.", paramName: nameof(id)
		);
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(invocation: async subject =>
		{
			TaskAssignedToWardCollection tasks = await subject.GetTasks();
			if (await tasks.AnyAsync(predicate: task => task.Id == e.TaskId))
				await subject.OnCompletedTask(e: e);
		}, filter: subject => subject.TasksAreCreated);

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(invocation: async subject =>
		{
			TaskAssignedToWardCollection tasks = await subject.GetTasks();
			if (await tasks.AnyAsync(predicate: task => task.Id == e.TaskId))
				await subject.OnUncompletedTask(e: e);
		}, filter: subject => subject.TasksAreCreated);

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedTask(e: e),
			filter: subject => (subject.Id == 0 || subject.Id == e.SubjectId) && subject.TasksAreCreated
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
		Func<WardSubjectStudying, Task> invocation,
		Predicate<WardSubjectStudying> filter
	)
	{
		if (!_subjects.IsValueCreated)
			return;

		List<WardSubjectStudying> collection = await _subjects;
		foreach (WardSubjectStudying subject in collection.FindAll(match: filter))
			await invocation(arg: subject);
	}
	#endregion

	#region IAsyncEnumerable<WardSubjectStudying>
	public async IAsyncEnumerator<WardSubjectStudying> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (WardSubjectStudying subject in await _subjects)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return subject;
		}
	}
	#endregion
	#endregion
}