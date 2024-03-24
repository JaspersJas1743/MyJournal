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
	#endregion

	#region Constructors
	private TaughtSubject(ApiClient client)
		=> _client = client;

	private TaughtSubject(
		ApiClient client,
		string name
	) : this(client: client)
	{
		Name = name;
		IsFirst = true;
	}

	private TaughtSubject(
		ApiClient client,
		TaughtSubjectResponse response
	) : this(client: client)
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

	#region Methods
	#region Static
	internal static TaughtSubject Create(
		ApiClient client,
		TaughtSubjectResponse response
	) => new TaughtSubject(client: client, response: response);

	internal static TaughtSubject Create(
		ApiClient client,
		string name
	) => new TaughtSubject(client: client, name: name);
	#endregion
	#endregion
}