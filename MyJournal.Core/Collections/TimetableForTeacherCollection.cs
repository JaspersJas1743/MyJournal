using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class TimetableForTeacherCollection : TimetableCollection<TimetableForTeacher>
{
	private TimetableForTeacherCollection(
		ApiClient client,
		AsyncLazy<Dictionary<DateOnly, TimetableForTeacher[]>> timetableOnDate
	) : base(client: client, timetableOnDate: timetableOnDate)
	{ }

	private sealed record GetTimetableResponse(
		SubjectOnTimetable Subject,
		BreakAfterSubject? Break
	) : ITResponse
	{
		public async Task<TimetableForTeacher> ConvertToT()
			=> await TimetableForTeacher.Create(subject: Subject, @break: Break);
	}

	public override async Task<TimetableForTeacher[]> GetByDate(
		DateOnly date,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await BaseGetByDate<GetTimetableResponse>(
			date: date,
			apiMethod: TimetableControllerMethods.GetTimetableByDateForTeacher,
			cancellationToken: cancellationToken
		);
	}

	internal static async Task<TimetableForTeacherCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TimetableForTeacherCollection(
			client: client,
			timetableOnDate: new AsyncLazy<Dictionary<DateOnly, TimetableForTeacher[]>>(valueFactory: async () =>
			{
				Dictionary<DateOnly, TimetableForTeacher[]> timetable = new Dictionary<DateOnly, TimetableForTeacher[]>();
				DateOnly date = DateOnly.FromDateTime(dateTime: DateTime.Now);
				foreach (DateOnly d in Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays))
				{
					IEnumerable<GetTimetableResponse> response = await client.GetAsync<IEnumerable<GetTimetableResponse>, GetTimetableByDateRequest>(
						apiMethod: TimetableControllerMethods.GetTimetableByDateForTeacher,
						argQuery: new GetTimetableByDateRequest(Day: d),
						cancellationToken: cancellationToken
					) ?? throw new InvalidOperationException();
					timetable.Add(key: d, value: await Task.WhenAll(tasks: response.Select(async r => await r.ConvertToT())));
				}
				return timetable;
			})
		);
	}
}