using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class TimetableForWardCollection : TimetableCollection<TimetableForStudent>
{
	private TimetableForWardCollection(
		ApiClient client,
		AsyncLazy<Dictionary<DateOnly, IEnumerable<TimetableForStudent>>> timetableOnDate
	) : base(
		client: client,
		timetableOnDate: timetableOnDate
	) { }


	public sealed record GetTimetableWithAssessmentsResponse(
		SubjectOnTimetable Subject,
		BreakAfterSubject? Break
	);

	public sealed record GetTimetableWithAssessmentsByDateResponse(
		DateOnly Date,
		IEnumerable<GetTimetableWithAssessmentsResponse> Timetable
	) : ITResponse
	{
		public KeyValuePair<DateOnly, IEnumerable<TimetableForStudent>> ConvertToT()
		{
			return new KeyValuePair<DateOnly, IEnumerable<TimetableForStudent>>(
				key: Date,
				value: Timetable.Select(selector: r => TimetableForStudent.Create(
				   subject: r.Subject,
				   @break: r.Break
				))
			);
		}
	}

	public override async Task<IEnumerable<TimetableForStudent>> GetByDate(
		DateOnly date,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await BaseGetByDate<GetTimetableWithAssessmentsByDateResponse>(
			date: date,
			apiMethod: TimetableControllerMethods.GetTimetableByDatesForParent,
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
			timetableOnDate: new AsyncLazy<Dictionary<DateOnly, IEnumerable<TimetableForStudent>>>(valueFactory: async () =>
			{
				DateOnly date = DateOnly.FromDateTime(dateTime: DateTime.Now);
				IEnumerable<DateOnly> dates = Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays);
				IEnumerable<GetTimetableWithAssessmentsByDateResponse> response = await client.GetAsync<IEnumerable<GetTimetableWithAssessmentsByDateResponse>, GetTimetableByDatesRequest>(
					apiMethod: TimetableControllerMethods.GetTimetableByDatesForParent,
					argQuery: new GetTimetableByDatesRequest(Days: dates),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return response.ToDictionary(
					keySelector: r => r.Date,
					elementSelector: r => r.Timetable.Select(
						selector: t => TimetableForStudent.Create(
							subject: t.Subject,
							@break: t.Break
						)
					)
				);
			})
		);
	}
}