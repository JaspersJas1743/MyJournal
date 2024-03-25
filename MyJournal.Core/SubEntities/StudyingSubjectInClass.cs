using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public sealed class StudyingSubjectInClass : Subject
{
	#region Fields
	private readonly ApiClient _client;
	#endregion

	#region Constructors
	private StudyingSubjectInClass(ApiClient client)
		=> _client = client;

	private StudyingSubjectInClass(
		ApiClient client,
		string name
	) : this(client: client)
	{
		Name = name;
		IsFirst = true;
	}

	private StudyingSubjectInClass(
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
	internal static StudyingSubjectInClass Create(
		ApiClient client,
		StudyingSubjectResponse response
	) => new StudyingSubjectInClass(client: client, response: response);

	internal static StudyingSubjectInClass Create(
		ApiClient client,
		string name
	) => new StudyingSubjectInClass(client: client, name: name);
	#endregion
	#endregion
}