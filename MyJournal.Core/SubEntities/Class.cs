using MyJournal.Core.Builders.TimetableBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public class Class : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<StudyingSubjectInClassCollection> _studyingSubjects;

	private AsyncLazy<IEnumerable<TimetableForClass>> _timetable;
	#endregion

	#region Constructors
	private Class(
		ApiClient client,
		int id,
		string name,
		AsyncLazy<StudyingSubjectInClassCollection> studyingSubjects,
		AsyncLazy<IEnumerable<TimetableForClass>> timetable
	)
	{
		Id = id;
		Name = name;
		_client = client;
		_studyingSubjects = studyingSubjects;
		_timetable = timetable;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string Name { get; init; }
	internal bool StudyingSubjectsAreCreated => _studyingSubjects.IsValueCreated;
	#endregion

	#region Records
	private sealed record GetTimetableByClassRequest(int ClassId);
	private sealed record GetTimetableByClassResponse(DayOfWeek DayOfWeek, int TotalHours, IEnumerable<SubjectInClassOnTimetable> Subjects);
	#endregion

	#region Events
	public event ChangedTimetableHandler ChangedTimetable;
	#endregion

	#region Methods
	#region Static
	private static async Task<AsyncLazy<IEnumerable<TimetableForClass>>> GetTimetable(
		ApiClient client,
		int classId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new AsyncLazy<IEnumerable<TimetableForClass>>(valueFactory: async () =>
		{
			IEnumerable<GetTimetableByClassResponse>? response = await client.GetAsync<IEnumerable<GetTimetableByClassResponse>, GetTimetableByClassRequest>(
				apiMethod: TimetableControllerMethods.GetTimetableForClass,
				argQuery: new GetTimetableByClassRequest(ClassId: classId),
				cancellationToken: cancellationToken
			);
			return await Task.WhenAll(tasks: response?.Select(selector: async r => await TimetableForClass.Create(
				dayOfWeek: r.DayOfWeek,
				totalHours: r.TotalHours,
				subjects: r.Subjects
			)) ?? Enumerable.Empty<Task<TimetableForClass>>());
		});
	}

	internal static async Task<Class> Create(
		ApiClient client,
		IFileService fileService,
		int classId,
		string name,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new Class(
			id: classId,
			name: name,
			client: client,
			studyingSubjects: new AsyncLazy<StudyingSubjectInClassCollection>(valueFactory: async () => await StudyingSubjectInClassCollection.Create(
				client: client,
				fileService: fileService,
				classId: classId,
				cancellationToken: cancellationToken
			)),
			timetable: await GetTimetable(client: client, classId: classId, cancellationToken: cancellationToken)
		);
	}
	#endregion

	#region Instance
	public async Task<StudyingSubjectInClassCollection> GetStudyingSubjects()
		=> await _studyingSubjects;

	public async Task<IEnumerable<TimetableForClass>> GetTimetable()
		=> await _timetable;

	public async Task<ITimetableBuilder> CreateTimetable()
		=> InitTimetableBuilder.Create(client: _client).ForClass(classId: Id, currentTimetable: await _timetable);

	internal async Task OnChangedTimetable(ChangedTimetableEventArgs e)
	{
		_timetable = await GetTimetable(client: _client, classId: e.ClassId);

		ChangedTimetable?.Invoke(e: e);
	}
	#endregion
	#endregion
}