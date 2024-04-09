using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Builders.TimetableBuilder;

internal sealed class TimetableBuilder : ITimetableBuilder
{
	private readonly ApiClient _client;
	private readonly int _classId;
	private readonly Dictionary<int, BaseTimetableForDayBuilder> _days;

	private TimetableBuilder(
		ApiClient client,
		int classId
	)
	{
		_client = client;
		_classId = classId;
		_days = new Dictionary<int, BaseTimetableForDayBuilder>();
	}

	private TimetableBuilder(
		ApiClient client,
		int classId,
		IEnumerable<TimetableForClass> currentTimetable
	) : this(client: client, classId: classId)
	{
		_days = currentTimetable.ToDictionary(
			keySelector: t => t.DayOfWeek.Id,
			elementSelector: t => TimetableForDayBuilder.Create(t.Subjects)
		);
	}

	private sealed record CreateTimetableRequest(int ClassId, IEnumerable<Timetable> Timetable);
	private sealed record SubjectOnTimetable(int Id, int Number, TimeSpan Start, TimeSpan End);
	private sealed record Timetable(int DayOfWeekId, IEnumerable<SubjectOnTimetable> Subjects);

	public BaseTimetableForDayBuilder ForDay(int dayOfWeekId)
	{
		if (_days.TryGetValue(key: dayOfWeekId, value: out BaseTimetableForDayBuilder? value))
			return value;

		BaseTimetableForDayBuilder builder = TimetableForDayBuilder.Create();
		_days.Add(key: dayOfWeekId, value: builder);
		return builder;
	}

	public async Task Save(CancellationToken cancellationToken = default(CancellationToken))
	{
		await _client.PutAsync<CreateTimetableRequest>(
			apiMethod: TimetableControllerMethods.CreateTimetable,
			arg: new CreateTimetableRequest(ClassId: _classId, Timetable: _days.Select(
				selector: d => new Timetable(DayOfWeekId: d.Key, Subjects: d.Value.Subjects.Select(
					selector: s => new SubjectOnTimetable(Id: s.SubjectId, Number: s.Number, Start: s.StartTime, End: s.EndTime)
				))
			)),
			cancellationToken: cancellationToken
		);
	}

	internal static ITimetableBuilder Create(ApiClient client, int classId)
		=> new TimetableBuilder(client: client, classId: classId);

	internal static ITimetableBuilder Create(ApiClient client, int classId, IEnumerable<TimetableForClass> currentTimetable)
		=> new TimetableBuilder(client: client, classId: classId, currentTimetable: currentTimetable);

	public IEnumerator<KeyValuePair<int, BaseTimetableForDayBuilder>> GetEnumerator()
		=> _days.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
}