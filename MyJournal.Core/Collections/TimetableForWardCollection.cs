using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class TimetableForWardCollection : TimetableCollection<TimetableForStudent>
{
	private TimetableForWardCollection(
		ApiClient client,
		AsyncLazy<Dictionary<DateOnly, TimetableForStudent[]>> timetableOnDate
	) : base(client: client, timetableOnDate: timetableOnDate)
	{ }

	private sealed record GetTimetableResponse(
		SubjectOnTimetable Subject,
		IEnumerable<EstimationOnTimetable> Estimations,
		BreakAfterSubject? Break
	) : ITResponse
	{
		public async Task<TimetableForStudent> ConvertToT()
			=> await TimetableForStudent.Create(subject: Subject, estimations: Estimations, @break: Break);
	}

	public override async Task<TimetableForStudent[]> GetByDate(
		DateOnly date,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await BaseGetByDate<GetTimetableResponse>(
			date: date,
			apiMethod: TimetableControllerMethods.GetTimetableByDateForParent,
			cancellationToken: cancellationToken
		);
	}

	internal static async Task<TimetableForWardCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TimetableForWardCollection(
			client: client,
			timetableOnDate: new AsyncLazy<Dictionary<DateOnly, TimetableForStudent[]>>(valueFactory: async () =>
			{
				Dictionary<DateOnly, TimetableForStudent[]> timetable = new Dictionary<DateOnly, TimetableForStudent[]>();
				DateOnly date = DateOnly.FromDateTime(dateTime: DateTime.Now);
				foreach (DateOnly d in Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays))
				{
					IEnumerable<GetTimetableResponse> response = await client.GetAsync<IEnumerable<GetTimetableResponse>, GetTimetableByDateRequest>(
						apiMethod: TimetableControllerMethods.GetTimetableByDateForParent,
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