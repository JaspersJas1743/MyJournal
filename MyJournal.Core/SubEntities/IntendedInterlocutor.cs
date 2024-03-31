using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class IntendedInterlocutor : ISubEntity
{
	#region Fields
	private readonly AsyncLazy<PersonalData> _personalData;
	private readonly AsyncLazy<ProfilePhoto> _photo;
	#endregion

	#region Constructor
	private IntendedInterlocutor(
		int id,
		AsyncLazy<PersonalData> personalData,
		AsyncLazy<ProfilePhoto> photo,
		Activity.Statuses activity,
		DateTime? onlineAt
	)
	{
		_personalData = personalData;
		_photo = photo;

		Id = id;
		Activity = activity;
		OnlineAt = onlineAt;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public Activity.Statuses Activity { get; init; }
	public DateTime? OnlineAt { get; init; }
	#endregion

	#region Records
	private sealed record InterlocutorResponse(int Id, string Surname, string Name, string? Patronymic, string Photo, Activity.Statuses Activity, DateTime? OnlineAt);
	#endregion

	#region Methods
	internal static async Task<IntendedInterlocutor> Create(
		ApiClient client,
		IFileService fileService,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		InterlocutorResponse response = await client.GetAsync<InterlocutorResponse>(
			apiMethod: UserControllerMethods.GetInformationAbout(userId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		IntendedInterlocutor interlocutor = new IntendedInterlocutor(
			id: response.Id,
			personalData: new AsyncLazy<PersonalData>(valueFactory: async () => new PersonalData(
				name: response.Name,
				surname: response.Surname,
				patronymic: response.Patronymic
			)),
			photo: new AsyncLazy<ProfilePhoto>(valueFactory: async () => new ProfilePhoto(
				client: client,
				fileService: fileService,
				link: response.Photo
			)),
			activity: response.Activity,
			onlineAt: response.OnlineAt
		);
		return interlocutor;
	}

	public async Task<PersonalData> GetPersonalData()
		=> await _personalData;

	public async Task<ProfilePhoto?> GetPhoto()
		=> await _photo;
	#endregion
}