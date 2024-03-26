using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class WardSubjectStudying : Subject
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<TaskAssignedToWardCollection> _tasks;
	#endregion

	#region Constructors
	private WardSubjectStudying(
		ApiClient client,
		Lazy<TaskAssignedToWardCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private WardSubjectStudying(
		ApiClient client,
		string name,
		Lazy<TaskAssignedToWardCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
		IsFirst = true;
	}

	private WardSubjectStudying(
		ApiClient client,
		StudyingSubjectResponse response,
		Lazy<TaskAssignedToWardCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Teacher = response.Teacher;
		IsFirst = false;
	}
	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	#endregion

	#region Properties
	public TaskAssignedToWardCollection Tasks => _tasks.Value;
	#endregion
	
	#region Methods
	#region Static
	internal static async Task<WardSubjectStudying> Create(
		ApiClient client,
		StudyingSubjectResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(client: client, response: response, tasks: new Lazy<TaskAssignedToWardCollection>(value:
			TaskAssignedToWardCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static async Task<WardSubjectStudying> Create(
		ApiClient client,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new WardSubjectStudying(client: client, name: name, tasks: new Lazy<TaskAssignedToWardCollection>(value:
			TaskAssignedToWardCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}
	#endregion
	#endregion
}