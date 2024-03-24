using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class SubjectTeacher : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
	public string Surname { get; init; }
	public string? Patronymic { get; init; }
}

public sealed class StudyingSubject : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	#endregion

	#region Constructors
	private StudyingSubject(ApiClient client)
		=> _client = client;

	private StudyingSubject(
		ApiClient client,
        string name
	) : this(client: client)
	{
		Name = name;
		IsFirst = true;
	}

	private StudyingSubject(
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

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	public SubjectTeacher Teacher { get; init; }
	internal bool IsFirst { get; init; }

	#endregion

	#region Records
	internal sealed record StudyingSubjectResponse(int Id, string Name, SubjectTeacher Teacher);
	#endregion

	#region Methods
	#region Static
	internal static StudyingSubject Create(
		ApiClient client,
		StudyingSubjectResponse response
	) => new StudyingSubject(client: client, response: response);

	internal static StudyingSubject Create(
		ApiClient client,
		string name
	) => new StudyingSubject(client: client, name: name);
	#endregion
	#endregion
}