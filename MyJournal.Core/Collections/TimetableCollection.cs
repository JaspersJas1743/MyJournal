using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.Collections;

public abstract class TimetableCollection<T>(ApiClient client, AsyncLazy<Dictionary<DateOnly, T[]>> timetableOnDate) : IAsyncEnumerable<KeyValuePair<DateOnly, T[]>>
{
	private readonly ApiClient _client = client;
	private readonly AsyncLazy<Dictionary<DateOnly, T[]>> _timetableOnDate = timetableOnDate;

	#region Interfaces
	protected interface ITResponse
	{
		Task<T> ConvertToT();
	}
	#endregion

	#region Records
	protected sealed record GetTimetableByDateRequest(DateOnly Day);
	#endregion

	#region Methods
	public abstract Task<T[]> GetByDate(DateOnly date, CancellationToken cancellationToken = default(CancellationToken));

	protected async Task<T[]> BaseGetByDate<TResponse>(
		DateOnly date,
		string apiMethod,
		CancellationToken cancellationToken = default(CancellationToken)
	) where TResponse : ITResponse
	{
		Dictionary<DateOnly, T[]> timetables = await _timetableOnDate;

		foreach (DateOnly d in Enumerable.Range(start: -3, count: 7).Select(selector: date.AddDays))
		{
			if (timetables.ContainsKey(key: d))
				continue;

			IEnumerable<TResponse> response = await _client.GetAsync<IEnumerable<TResponse>, GetTimetableByDateRequest>(
				apiMethod: apiMethod,
				argQuery: new GetTimetableByDateRequest(Day: d),
				cancellationToken: cancellationToken
			) ?? throw new InvalidOperationException();
			timetables.Add(key: d, value: await Task.WhenAll(tasks: response.Select(async r => await r.ConvertToT())));
		}

		return timetables[key: date];
	}
	#endregion

	public async IAsyncEnumerator<KeyValuePair<DateOnly, T[]>> GetAsyncEnumerator(
		CancellationToken cancellationToken = new CancellationToken()
	)
	{
		foreach (KeyValuePair<DateOnly, T[]> pair in await _timetableOnDate)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return pair;
		}
	}
}