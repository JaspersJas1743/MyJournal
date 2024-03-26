using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubject : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<AssignedTaskCollection> _tasks;
	#endregion

	#region Constructors

	private StudyingSubject(
		ApiClient client,
		Lazy<AssignedTaskCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private StudyingSubject(
		ApiClient client,
		string name,
		Lazy<AssignedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
		IsFirst = true;
	}

	private StudyingSubject(
		ApiClient client,
		StudyingSubjectResponse response,
		Lazy<AssignedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
		IsFirst = false;
	}
	#endregion

	#region Properties
	public AssignedTaskCollection Tasks => _tasks.Value;
	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	#endregion

	#region Methods
	#region Static
	internal static async Task<StudyingSubject> Create(
		ApiClient client,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(client: client, response: response, tasks: new Lazy<AssignedTaskCollection>(value:
			AssignedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static async Task<StudyingSubject> Create(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new StudyingSubject(client: client, name: name, tasks: new Lazy<AssignedTaskCollection>(value:
			AssignedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}
	#endregion
	#endregion
}