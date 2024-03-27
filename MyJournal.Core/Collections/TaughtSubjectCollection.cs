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
	private readonly Lazy<List<TaughtSubject>> _subjects;
	#endregion

	#region Constructor
	private TaughtSubjectCollection(
		ApiClient client,
		IFileService fileService,
		IEnumerable<TaughtSubject> studyingSubjects
	)
	{
		_client = client;
		List<TaughtSubject> subjects = new List<TaughtSubject>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: TaughtSubject.Create(
			client: client,
			name: "Все дисциплины",
			fileService: fileService
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<TaughtSubject>>(value: subjects);
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

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
	public static async Task<TaughtSubjectCollection> Create(
		ApiClient client,
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<TaughtSubject.TaughtSubjectResponse> subjects = await client.GetAsync<IEnumerable<TaughtSubject.TaughtSubjectResponse>>(
			apiMethod: LessonControllerMethods.GetTaughtSubjects,
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
			).GetAwaiter().GetResult()
		));
	}
	#endregion

	#region Instance
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