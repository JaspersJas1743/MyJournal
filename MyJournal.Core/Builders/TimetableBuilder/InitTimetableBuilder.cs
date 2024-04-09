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

	internal static IInitTimetableBuilder Create(ApiClient client)
		=> new InitTimetableBuilder(client: client);
}