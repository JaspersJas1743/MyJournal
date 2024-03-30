using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public class Class : ISubEntity
{
	private readonly AsyncLazy<StudyingSubjectInClassCollection> _studyingSubjects;

	private Class(
		int id,
		string name,
		AsyncLazy<StudyingSubjectInClassCollection> studyingSubjects
	)
	{
		Id = id;
		Name = name;
		_studyingSubjects = studyingSubjects;
	}

	public int Id { get; init; }
	public string Name { get; init; }
	public async Task<StudyingSubjectInClassCollection> GetStudyingSubjects()
		=> await _studyingSubjects;

	internal static async Task<Class> Create(
		ApiClient client,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new Class(id: classId, name: name, studyingSubjects: new AsyncLazy<StudyingSubjectInClassCollection>(
			valueFactory: async () => await StudyingSubjectInClassCollection.Create(
				client: client,
				classId: classId,
				cancellationToken: cancellationToken
			)
		));
	}
}