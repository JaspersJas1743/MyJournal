using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class ClassCollection : IEnumerable<Class>
{
	private readonly AsyncLazy<List<Class>> _classes;

	private ClassCollection(
		AsyncLazy<List<Class>> classes
	)
	{
		_classes = classes;
	}

	public async Task<Class> GetByIndex(int index)
	{
		List<Class> collection = await _classes;
		return collection.ElementAtOrDefault(index: index) ?? throw new ArgumentOutOfRangeException(
			message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index)
		);
	}

	private sealed record GetClassesResponse(int Id, string Name);

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

	#region Instance
	internal async Task OnCompletedTask(CompletedTaskEventArgs e)
	{
		// foreach (Class @class in this.Where(predicate: c => c.StudyingSubjects.Any(predicate: s => s.Tasks.Any(predicate: t => t.Id == e.TaskId))))
		// 	foreach (StudyingSubjectInClass subject in @class.StudyingSubjects.Where(predicate: ss => ss.Tasks.Any(predicate: t => t.Id == e.TaskId)))
		// 		if (subject.Tasks.IsLoaded)
		// 			await subject.OnCompletedTask(e: new StudyingSubjectInClass.CompletedTaskEventArgs(taskId: e.TaskId));

		CompletedTask?.Invoke(e: e);
	}

	internal async Task OnUncompletedTask(UncompletedTaskEventArgs e)
	{
		// foreach (Class @class in this.Where(predicate: c => c.StudyingSubjects.Any(predicate: s => s.Tasks.Any(predicate: t => t.Id == e.TaskId))))
		// 	foreach (StudyingSubjectInClass subject in @class.StudyingSubjects.Where(predicate: ss => ss.Tasks.Any(predicate: t => t.Id == e.TaskId)))
		// 		if (subject.Tasks.IsLoaded)
		// 			await subject.OnUncompletedTask(e: new StudyingSubjectInClass.UncompletedTaskEventArgs(taskId: e.TaskId));

		UncompletedTask?.Invoke(e: e);
	}

	internal async Task OnCreatedTask(CreatedTaskEventArgs e)
	{
		// foreach (Class @class in this.Where(predicate: c => c.Id == e.ClassId))
		// 	foreach (StudyingSubjectInClass subject in @class.StudyingSubjects.Where(predicate: ss => ss.Id == 0 || ss.Id == e.SubjectId))
		// 		if (subject.Tasks.IsLoaded)
		// 			await subject.OnCreatedTask(e: new StudyingSubjectInClass.CreatedTaskEventArgs(taskId: e.TaskId));

		CreatedTask?.Invoke(e: e);
	}
	#endregion

	public IEnumerator<Class> GetEnumerator()
		=> _classes.GetAwaiter().GetResult().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
}