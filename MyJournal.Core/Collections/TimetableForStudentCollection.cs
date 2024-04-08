using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class TimetableForStudentCollection : TimetableCollection<TimetableForStudent>
{
	private TimetableForStudentCollection(
		ApiClient client,
		AsyncLazy<Dictionary<DateOnly, TimetableForStudent[]>> timetableOnDate
	) : base(client: client, timetableOnDate: timetableOnDate)
	{ }

	private sealed record GetTimetableWithAssessmentsResponse(
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
		return await BaseGetByDate<GetTimetableWithAssessmentsResponse>(
			date: date,
			apiMethod: TimetableControllerMethods.GetTimetableByDateForStudent,
			cancellationToken: cancellationToken
		);
	}

	internal static async Task<TimetableForStudentCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new TimetableForStudentCollection(
			client: client,
			timetableOnDate: new AsyncLazy<Dictionary<DateOnly, TimetableForStudent[]>>(valueFactory: async () =>
			{
				Dictionary<DateOnly, TimetableForStudent[]> timetable = new Dictionary<DateOnly, TimetableForStudent[]>();
				DateOnly date = DateOnly.FromDateTime(dateTime: DateTime.Now);
				foreach (DateOnly d in Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays))
				{
					IEnumerable<GetTimetableWithAssessmentsResponse> response = await client.GetAsync<IEnumerable<GetTimetableWithAssessmentsResponse>, GetTimetableByDateRequest>(
						apiMethod: TimetableControllerMethods.GetTimetableByDateForStudent,
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