using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class ClassCollection : IAsyncEnumerable<Class>
{
	#region Fields
	private readonly AsyncLazy<List<Class>> _classes;
	#endregion

	#region Constructors
	private ClassCollection(
		AsyncLazy<List<Class>> classes
	) => _classes = classes;

	#endregion

	#region Records
	private sealed record GetClassesResponse(int Id, string Name);
	#endregion

	#region Events
	public event CompletedTaskHandler CompletedTask;
	public event UncompletedTaskHandler UncompletedTask;
	public event CreatedTaskHandler CreatedTask;
	public event CreatedAssessmentHandler CreatedAssessment;
	public event ChangedAssessmentHandler ChangedAssessment;
	public event DeletedAssessmentHandler DeletedAssessment;
	#endregion

	#region Methods
	#region Static
	internal static async Task<ClassCollection> Create(
		ApiClient client,
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetClassesResponse> classes = await client.GetAsync<IEnumerable<GetClassesResponse>>(
			apiMethod: ClassControllerMethods.GetClasses,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new ClassCollection(classes: new AsyncLazy<List<Class>>(valueFactory: async () => new List<Class>(collection: await Task.WhenAll(
			tasks: classes.Select(async c => await Class.Create(
				client: client,
				fileService: fileService,
				classId: c.Id,
				name: c.Name,
				cancellationToken: cancellationToken
			))
		))));
	}
	#endregion

	#region Instance
	public async Task<Class> GetByIndex(int index)
	{
		List<Class> collection = await _classes;
		return collection.ElementAtOrDefault(index: index) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index)
		);
	}

	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCompletedTask(e: e),
			taskFilter: subject => subject.Id == e.TaskId
		);

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnUncompletedTask(e: e),
			taskFilter: subject => subject.Id == e.TaskId
		);

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedTask(e: e),
			classFilter: @class => @class.Id == e.ClassId,
			subjectFilter: subject => (subject.Id == 0 || subject.Id == e.SubjectId) && subject.TasksAreCreated
		);

		CreatedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedAssessment(CreatedAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedAssessment(e: e),
			subjectFilter: subject => subject.Id == e.SubjectId
		);

		CreatedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedAssessment(ChangedAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnChangedAssessment(e: e),
			subjectFilter: subject => subject.Id == e.SubjectId
		);

		ChangedAssessment?.Invoke(e: e);
	}

	internal async Task OnDeletedAssessment(DeletedAssessmentEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnDeletedAssessment(e: e),
			subjectFilter: subject => subject.Id == e.SubjectId
		);

		DeletedAssessment?.Invoke(e: e);
	}

	internal async Task OnChangedTimetable(ChangedTimetableEventArgs e)
	{
		List<Class> classes = await _classes;
		await classes.Find(match: c => c.Id == e.ClassId)!.OnChangedTimetable(e: e);
	}

	private async Task InvokeIfSubjectsAreCreated(
		Func<StudyingSubjectInClass, Task> invocation,
		Predicate<Class> classFilter,
		Func<StudyingSubjectInClass, bool> subjectFilter
	)
	{
		if (!_classes.IsValueCreated)
			return;

		List<Class> collection = await _classes;
		StudyingSubjectInClassCollection[] subjectCollection = await Task.WhenAll(
			tasks: collection.FindAll(match: @class => @class.StudyingSubjectsAreCreated && classFilter(obj: @class))
				.Select<Class, Task<StudyingSubjectInClassCollection>>(selector: async @class => await @class.GetStudyingSubjects())
		);
		foreach (StudyingSubjectInClassCollection subjects in subjectCollection)
		{
			await foreach (StudyingSubjectInClass subject in subjects.Where(predicate: subjectFilter))
				await invocation(arg: subject);
		}
	}

	private async Task InvokeIfSubjectsAreCreated(
		Func<StudyingSubjectInClass, Task> invocation,
		Func<StudyingSubjectInClass, bool> subjectFilter
	) => await InvokeIfSubjectsAreCreated(invocation: invocation, classFilter: _ => true, subjectFilter: subjectFilter);

	private async Task InvokeIfSubjectsAreCreated(
		Func<StudyingSubjectInClass, Task> invocation,
		Func<TaskAssignedToClass, bool> taskFilter
	)
	{
		if (!_classes.IsValueCreated)
			return;

		List<Class> collection = await _classes;
		StudyingSubjectInClassCollection[] subjectCollection = await Task.WhenAll(
			tasks: collection.FindAll(match: @class => @class.StudyingSubjectsAreCreated)
				.Select(selector: async @class => await @class.GetStudyingSubjects())
		);
		foreach (StudyingSubjectInClassCollection subjects in subjectCollection)
		{
			await foreach (StudyingSubjectInClass subject in subjects.Where(predicate: s => s.TasksAreCreated))
			{
				TaskAssignedToClassCollection tasks = await subject.GetTasks();
				if (await tasks.AnyAsync(predicate: taskFilter))
					await invocation(arg: subject);
			}
		}
	}
	#endregion

	#region IAsyncEnumerable<Class>
	public async IAsyncEnumerator<Class> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (Class @class in await _classes)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return @class;
		}
	}
	#endregion
	#endregion
}