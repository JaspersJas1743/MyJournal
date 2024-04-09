using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.Builders.TimetableBuilder;

internal sealed class InitTimetableBuilder : IInitTimetableBuilder
{
	private readonly ApiClient _client;

	private InitTimetableBuilder(ApiClient client)
	{
		_client = client;
	}

	public ITimetableBuilder ForClass(int classId)
		=> TimetableBuilder.Create(client: _client, classId: classId);

	public ITimetableBuilder ForClass(int classId, IEnumerable<TimetableForClass> currentTimetable)
		=> TimetableBuilder.Create(client: _client, classId: classId, currentTimetable: currentTimetable);

	internal static IInitTimetableBuilder Create(ApiClient client)
		=> new InitTimetableBuilder(client: client);
}