using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Interlocutors;

public sealed class Interlocutor
{
	public int Id { get; init; }
	public string? Surname { get; init; }
	public string? Name { get; init; }
	public string? Patronymic { get; init; }
	public string? Photo { get; init; }
	public Activity.Statuses Activity { get; init; }
	public DateTime? OnlineAt { get; init; }

	public static async Task<Interlocutor> Create(
	internal static async Task<Interlocutor> Create(
		ApiClient client,
		IFileService fileService,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Interlocutor response = await client.GetAsync<Interlocutor>(
			apiMethod: UserControllerMethods.GetInformationAbout(userId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response;
	}
}