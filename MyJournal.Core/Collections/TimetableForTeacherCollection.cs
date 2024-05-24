using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class TimetableForTeacherCollection : TimetableCollection<TimetableForTeacher>
{
	private TimetableForTeacherCollection(
		ApiClient client,
		AsyncLazy<Dictionary<DateOnly, IEnumerable<TimetableForTeacher>>> timetableOnDate
	) : base(
		client: client,
		timetableOnDate: timetableOnDate
	)
	{ }

	public sealed record GetTimetableWithoutAssessmentsResponse(
		SubjectOnTimetable Subject,
		BreakAfterSubject? Break
	);

	public sealed record GetTimetableWithoutAssessmentsByDateResponse(
		DateOnly Date,
		IEnumerable<GetTimetableWithoutAssessmentsResponse> Timetable
	) : ITResponse
	{
		public KeyValuePair<DateOnly, IEnumerable<TimetableForTeacher>> ConvertToT()
		{
			return new KeyValuePair<DateOnly, IEnumerable<TimetableForTeacher>>(
				key: Date,
				value: Timetable.Select(selector: r => TimetableForTeacher.Create(
					subject: r.Subject,
					@break: r.Break
				))
			);
		}
	}

	public override async Task<IEnumerable<TimetableForTeacher>> GetByDate(
		DateOnly date,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await BaseGetByDate<GetTimetableWithoutAssessmentsByDateResponse>(
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
			timetableOnDate: new AsyncLazy<Dictionary<DateOnly, IEnumerable<TimetableForTeacher>>>(valueFactory: async () =>
			{
				DateOnly date = DateOnly.FromDateTime(dateTime: DateTime.Now);
				IEnumerable<DateOnly> dates = Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays);
				IEnumerable<GetTimetableWithoutAssessmentsByDateResponse> response = await client.GetAsync<IEnumerable<GetTimetableWithoutAssessmentsByDateResponse>, GetTimetableByDatesRequest>(
					apiMethod: TimetableControllerMethods.GetTimetableByDatesForTeacher,
					argQuery: new GetTimetableByDatesRequest(Days: dates),
					cancellationToken: cancellationToken
				) ?? throw new InvalidOperationException();
				return response.ToDictionary(
					keySelector: r => r.Date,
					elementSelector: r => r.Timetable.Select(
						selector: t => TimetableForTeacher.Create(
							subject: t.Subject,
							@break: t.Break
						)
					)
				);
			})
		);
	}
}