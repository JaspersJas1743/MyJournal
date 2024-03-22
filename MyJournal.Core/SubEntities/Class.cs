using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public class Class : ISubEntity
{
	private readonly Lazy<StudyingSubjectCollection> _studyingSubjects;

	private Class(
		int id,
		string name,
		StudyingSubjectCollection studyingSubjects
	)
	{
		Id = id;
		Name = name;
		_studyingSubjects = new Lazy<StudyingSubjectCollection>(value: studyingSubjects);
	}

	public int Id { get; init; }
	public string Name { get; init; }
	public StudyingSubjectCollection StudyingSubjects => _studyingSubjects.Value;

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
			studyingSubjects: await StudyingSubjectCollection.Create(
				client: client,
				apiMethod: LessonControllerMethods.GetSubjectsStudiedInClass(classId: classId),
				cancellationToken: cancellationToken
			)
		);
	}
}