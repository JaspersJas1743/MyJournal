using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
}

public sealed class TaughtSubject : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<CreatedTaskCollection> _tasks;
	#endregion

	#region Constructors
	private TaughtSubject(
		ApiClient client,
		Lazy<CreatedTaskCollection> tasks
	)
	{
		_client = client;
		_tasks = tasks;
	}

	private TaughtSubject(
		ApiClient client,
		string name,
		Lazy<CreatedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Name = name;
		IsFirst = true;
	}

	private TaughtSubject(
		ApiClient client,
		TaughtSubjectResponse response,
		Lazy<CreatedTaskCollection> tasks
	) : this(client: client, tasks: tasks)
	{
		Id = response.Id;
		Name = response.Name;
		Class = response.Class;
		IsFirst = false;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	public TaughtClass Class { get; init; }
	internal bool IsFirst { get; init; }
	#endregion

	#region Records
	internal sealed record TaughtSubjectResponse(int Id, string Name, TaughtClass Class);
	#endregion

	#region Properties
	public CreatedTaskCollection Tasks => _tasks.Value;
	#endregion

	#region Methods
	#region Static
	internal static async Task<TaughtSubject> Create(
		ApiClient client,
		TaughtSubjectResponse response,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(client: client, response: response, tasks: new Lazy<CreatedTaskCollection>(value:
			CreatedTaskCollection.Create(
				client: client,
				subjectId: response.Id,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}

	internal static async Task<TaughtSubject> Create(
		ApiClient client,
		string name,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TaughtSubject(client: client, name: name, tasks: new Lazy<CreatedTaskCollection>(value:
			CreatedTaskCollection.Create(
				client: client,
				subjectId: 0,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
	}
	#endregion
	#endregion
}