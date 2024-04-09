using System.Diagnostics;
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

	private sealed record CreateTimetableRequest(int ClassId, IEnumerable<Timetable> Timetable)
	{
		public override string ToString() => $"[ClassId={ClassId},Timetable={{{String.Join(", ", Timetable)}}}]";
	}

	private sealed record SubjectOnTimetable(int Id, int Number, TimeSpan Start, TimeSpan End)
	{
		public override string ToString() => $"[Id={Id},Number={Number},Start={Start},End={End}]";
	}

	private sealed record Timetable(int DayOfWeekId, IEnumerable<SubjectOnTimetable> Subjects)
	{
		public override string ToString() => $"[DayOfWeekId={DayOfWeekId},Subjects={{{String.Join(", ", Subjects)}}}]";
	};

	public BaseTimetableForDayBuilder ForDay(int dayOfWeekId)
	{
		BaseTimetableForDayBuilder builder = TimetableForDayBuilder.Create();
		_days.Add(key: dayOfWeekId, value: builder);
		return builder;
	}

	public async Task Save(CancellationToken cancellationToken = default(CancellationToken))
	{
		var request = new CreateTimetableRequest(ClassId: _classId, Timetable: _days.Select(
			selector: d => new Timetable(DayOfWeekId: d.Key, Subjects: d.Value.Subjects.Select(
				selector: s => new SubjectOnTimetable(Id: s.SubjectId, Number: s.Number, Start: s.StartTime, End: s.EndTime)
			))
		));
		Debug.WriteLine(request);
		await _client.PutAsync<CreateTimetableRequest>(
			apiMethod: TimetableControllerMethods.CreateTimetable,
			arg: request,
			cancellationToken: cancellationToken
		);
	}

	internal static ITimetableBuilder Create(ApiClient client, int classId)
		=> new TimetableBuilder(client: client, classId: classId);
}