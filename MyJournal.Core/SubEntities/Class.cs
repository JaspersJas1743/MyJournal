using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.SubEntities;

public class Class : ISubEntity
{
	#region Fields
	private readonly AsyncLazy<StudyingSubjectInClassCollection> _studyingSubjects;
	#endregion

	#region Constructors
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
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	internal bool StudyingSubjectsAreCreated => _studyingSubjects.IsValueCreated;
	#endregion

	#region Methods
	#region Static
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
	#endregion

	#region Instance
	public async Task<StudyingSubjectInClassCollection> GetStudyingSubjects()
		=> await _studyingSubjects;
	#endregion
	#endregion
}