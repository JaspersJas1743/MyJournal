using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class Interlocutor : ISubEntity
{
	#region Constructor
	private Interlocutor(
		int id,
		PersonalData personalData,
		ProfilePhoto? photo,
		Activity.Statuses activity,
		DateTime? onlineAt
	)
	{
		Id = id;
		PersonalData = personalData;
		Photo = photo;
		Activity = activity;
		OnlineAt = onlineAt;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public PersonalData PersonalData { get; init; }
	public ProfilePhoto? Photo { get; init; }
	public Activity.Statuses Activity { get; init; }
	public DateTime? OnlineAt { get; init; }
	#endregion

	#region Records
	private sealed record InterlocutorResponse(int Id, string Surname, string Name, string? Patronymic, string Photo, Activity.Statuses Activity, DateTime? OnlineAt);
	#endregion

	#region Classes
	public sealed class AppearedOnlineEventArgs(DateTime? onlineAt) : EventArgs
	{
		public DateTime? OnlineAt { get; } = onlineAt;
	}

	public sealed class AppearedOfflineEventArgs(DateTime? onlineAt) : EventArgs
	{
		public DateTime? OnlineAt { get; } = onlineAt;
	}

	public sealed class UpdatedPhotoEventArgs : EventArgs;

	public sealed class DeletedPhotoEventArgs : EventArgs;
	#endregion

	#region Delegates
	public delegate void AppearedOnlineHandler(AppearedOnlineEventArgs e);
	public delegate void AppearedOfflineHandler(AppearedOfflineEventArgs e);
	public delegate void UpdatedPhotoHandler(UpdatedPhotoEventArgs e);
	public delegate void DeletedPhotoHandler(DeletedPhotoEventArgs e);
	#endregion

	#region Events
	public event AppearedOnlineHandler? AppearedOnline;
	public event AppearedOfflineHandler? AppearedOffline;
	public event UpdatedPhotoHandler? UpdatedPhoto;
	public event DeletedPhotoHandler? DeletedPhoto;
	#endregion

	#region Methods
	#region Static
	internal static async Task<Interlocutor> Create(
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
		Interlocutor interlocutor = new Interlocutor(
			id: response.Id,
			personalData: new PersonalData(
				name: response.Name,
				surname: response.Surname,
				patronymic: response.Patronymic
			),
			photo: new ProfilePhoto(
				client: client,
				fileService: fileService,
				link: response.Photo
			),
			activity: response.Activity,
			onlineAt: response.OnlineAt
		);
		return interlocutor;
	}
	#endregion

	#region Instance
	internal void OnAppearedOnline(AppearedOnlineEventArgs e)
		=> AppearedOnline?.Invoke(e: e);

	internal void OnAppearedOffline(AppearedOfflineEventArgs e)
		=> AppearedOffline?.Invoke(e: e);

	internal void OnUpdatedPhoto(UpdatedPhotoEventArgs e)
		=> UpdatedPhoto?.Invoke(e: e);

	internal void OnDeletedPhoto(DeletedPhotoEventArgs e)
		=> DeletedPhoto?.Invoke(e: e);
	#endregion
	#endregion
}