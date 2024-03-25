using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.SubEntities;

public class Class : ISubEntity
{
	private readonly Lazy<StudyingSubjectInClassCollection> _studyingSubjects;

	private Class(
		int id,
		string name,
		StudyingSubjectInClassCollection studyingSubjects
	)
	{
		Id = id;
		Name = name;
		_studyingSubjects = new Lazy<StudyingSubjectInClassCollection>(value: studyingSubjects);
	}

	public int Id { get; init; }
	public string Name { get; init; }
	public StudyingSubjectInClassCollection StudyingSubjects => _studyingSubjects.Value;

	internal static async Task<Class> Create(
		ApiClient client,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new Class(
			id: classId,
			name: name,
			studyingSubjects: await StudyingSubjectInClassCollection.Create(
				client: client,
				classId: classId,
				cancellationToken: cancellationToken
			)
		);
	}
}