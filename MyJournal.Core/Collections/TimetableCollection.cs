using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.Collections;

public abstract class TimetableCollection<T>(
	ApiClient client,
	AsyncLazy<Dictionary<DateOnly, IEnumerable<T>>> timetableOnDate
) : IAsyncEnumerable<KeyValuePair<DateOnly, IEnumerable<T>>>
{
	#region Interfaces
	protected interface ITResponse
	{
		KeyValuePair<DateOnly, IEnumerable<T>> ConvertToT();
	}
	#endregion

	#region Records
	protected sealed record GetTimetableByDateRequest(DateOnly Day);
	protected sealed record GetTimetableByDatesRequest(IEnumerable<DateOnly> Days);
	#endregion

	#region Methods
	public abstract Task<IEnumerable<T>> GetByDate(DateOnly date, CancellationToken cancellationToken = default(CancellationToken));

	protected async Task<IEnumerable<T>> BaseGetByDate<TResponse>(
		DateOnly date,
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	) where TResponse : ITResponse
	{
		Dictionary<DateOnly, IEnumerable<T>> timetables = await timetableOnDate;

		IEnumerable<DateOnly> dates = Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays).Except(second: timetables.Keys);
		if (!dates.Any())
			return timetables[key: date];

		IEnumerable<TResponse> response = await client.GetAsync<IEnumerable<TResponse>, GetTimetableByDatesRequest>(
			apiMethod: apiMethod,
			argQuery: new GetTimetableByDatesRequest(Days: dates),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		foreach (TResponse r in response)
		{
			KeyValuePair<DateOnly, IEnumerable<T>> pair = r.ConvertToT();
			timetables.Add(key: pair.Key, value: pair.Value);
		}

		return timetables[key: date];
	}
	#endregion

	public async IAsyncEnumerator<KeyValuePair<DateOnly, IEnumerable<T>>> GetAsyncEnumerator(
		CancellationToken cancellationToken = new CancellationToken()
	)
	{
		foreach (KeyValuePair<DateOnly, IEnumerable<T>> pair in await timetableOnDate)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return pair;
		}
	}
}