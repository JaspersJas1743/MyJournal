using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class ClassCollection : IEnumerable<Class>
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

	#region Classes
	public sealed class CompletedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	public sealed class UncompletedTaskEventArgs(int taskId) : EventArgs
	{
		public int TaskId { get; } = taskId;
	}
	public sealed class CreatedTaskEventArgs(int taskId, int subjectId, int classId) : EventArgs
	{
		public int TaskId { get; } = taskId;
		public int SubjectId { get; } = subjectId;
		public int ClassId { get; } = classId;
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
	internal static async Task<ClassCollection> Create(
		ApiClient client,
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
			invocation: async subject => await subject.OnCompletedTask(e: new StudyingSubjectInClass.CompletedTaskEventArgs(taskId: e.TaskId)),
			taskFilter: subject => subject.Id == e.TaskId
		);

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnUncompletedTask(e: new StudyingSubjectInClass.UncompletedTaskEventArgs(taskId: e.TaskId)),
			taskFilter: subject => subject.Id == e.TaskId
		);

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		await InvokeIfSubjectsAreCreated(
			invocation: async subject => await subject.OnCreatedTask(e: new StudyingSubjectInClass.CreatedTaskEventArgs(taskId: e.TaskId)),
			classFilter: @class => @class.Id == e.ClassId,
			subjectFilter: subject => subject.Id == 0 || subject.Id == e.SubjectId
		);

		CreatedTask?.Invoke(e: e);
	}

	private async Task InvokeIfSubjectsAreCreated(
		Func<StudyingSubjectInClass, Task> invocation,
		Predicate<Class> classFilter,
		Predicate<StudyingSubjectInClass> subjectFilter
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
			foreach (StudyingSubjectInClass subject in subjects.Where(predicate: s => s.TasksAreCreated && subjectFilter(obj: s)))
				await invocation(arg: subject);
		}
	}

	private async Task InvokeIfSubjectsAreCreated(
		Func<StudyingSubjectInClass, Task> invocation,
		Func<TaskAssignedToClass, bool> taskFilter
	)
	{
		if (!_classes.IsValueCreated)
			return;

		List<Class> collection = await _classes;
		StudyingSubjectInClassCollection[] subjectCollection = await Task.WhenAll(
			tasks: collection.FindAll(match: @class => @class.StudyingSubjectsAreCreated).Select(selector: async @class => await @class.GetStudyingSubjects())
		);
		foreach (StudyingSubjectInClassCollection subjects in subjectCollection)
		{
			foreach (StudyingSubjectInClass subject in subjects.Where(predicate: s => s.TasksAreCreated))
			{
				TaskAssignedToClassCollection tasks = await subject.GetTasks();
				if (tasks.Any(predicate: taskFilter))
					await invocation(arg: subject);
			}
		}
	}
	#endregion

	#region IEnumerable<Class>
	public IEnumerator<Class> GetEnumerator()
		=> _classes.GetAwaiter().GetResult().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion
	#endregion
}