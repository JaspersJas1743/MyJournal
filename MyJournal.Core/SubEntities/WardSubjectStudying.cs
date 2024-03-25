using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class WardSubjectStudying : Subject
{
	#region Fields
	private readonly ApiClient _client;
	#endregion

	#region Constructors
	private WardSubjectStudying(ApiClient client)
		=> _client = client;

	private WardSubjectStudying(
		ApiClient client,
		string name
	) : this(client: client)
	{
		Name = name;
		IsFirst = true;
	}

	private WardSubjectStudying(
		ApiClient client,
		StudyingSubjectResponse response
	) : this(client: client)
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

	#region Methods
	#region Static
	internal static WardSubjectStudying Create(
		ApiClient client,
		StudyingSubjectResponse response
	) => new WardSubjectStudying(client: client, response: response);

	internal static WardSubjectStudying Create(
		ApiClient client,
		string name
	) => new WardSubjectStudying(client: client, name: name);
	#endregion
	#endregion
}