using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class IntendedInterlocutor : ISubEntity
{
	#region Fields
	private readonly Lazy<PersonalData> _personalData;
	private readonly Lazy<ProfilePhoto> _photo;
	#endregion

	#region Constructor
	private IntendedInterlocutor(
		int id,
		Lazy<PersonalData> personalData,
		Lazy<ProfilePhoto> photo,
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
	public PersonalData PersonalData => _personalData.Value;
	public ProfilePhoto? Photo => _photo.Value;
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
			personalData: new Lazy<PersonalData>(value: new PersonalData(
				name: response.Name,
				surname: response.Surname,
				patronymic: response.Patronymic
			)),
			photo: new Lazy<ProfilePhoto>(value: new ProfilePhoto(
				client: client,
				fileService: fileService,
				link: response.Photo
			)),
			activity: response.Activity,
			onlineAt: response.OnlineAt
		);
		return interlocutor;
	}
	#endregion
}