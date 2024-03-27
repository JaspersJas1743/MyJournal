using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class StudyingSubjectCollection : IEnumerable<StudyingSubject>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<StudyingSubject>> _subjects;
	#endregion

	#region Constructor
	private StudyingSubjectCollection(
		ApiClient client,
		IEnumerable<StudyingSubject> studyingSubjects
	)
	{
		_client = client;
		List<StudyingSubject> subjects = new List<StudyingSubject>(collection: studyingSubjects);
		subjects.Insert(index: 0, item: StudyingSubject.Create(
			client: client,
			name: "Все дисциплины"
		).GetAwaiter().GetResult());
		_subjects = new Lazy<List<StudyingSubject>>(value: subjects);
	}
	#endregion

	#region Properties
	public int Length => _subjects.Value.Count;

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
	#region Instance
	public static async Task<StudyingSubjectCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<StudyingSubject.StudyingSubjectResponse> subjects = await client.GetAsync<IEnumerable<StudyingSubject.StudyingSubjectResponse>>(
			apiMethod: LessonControllerMethods.GetStudyingSubjects,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new StudyingSubjectCollection(
			client: client,
			studyingSubjects: subjects.Select(selector: s =>
				StudyingSubject.Create(
					client: client,
					response: s,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			)
		);
	}
	#endregion

	#region Instance
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