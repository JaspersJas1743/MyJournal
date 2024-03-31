using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class Interlocutor : ISubEntity
{
	#region Fields
	private readonly AsyncLazy<PersonalData> _personalData;
	private readonly AsyncLazy<ProfilePhoto> _photo;
	#endregion

	#region Constructor
	private Interlocutor(
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
	public Activity.Statuses Activity { get; private set; }
	public DateTime? OnlineAt { get; private set; }
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

	public sealed class UpdatedPhotoEventArgs(string link) : EventArgs
	{
		public string Link { get; } = link;
	}

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
	#endregion

	#region Instance
	public async Task<PersonalData> GetPersonalData()
		=> await _personalData;

	public async Task<ProfilePhoto> GetPhoto()
		=> await _photo;

	internal void OnAppearedOnline(AppearedOnlineEventArgs e)
	{
		Activity = UserData.Activity.Statuses.Online;
		OnlineAt = e.OnlineAt;
		AppearedOnline?.Invoke(e: e);
	}

	internal void OnAppearedOffline(AppearedOfflineEventArgs e)
	{
		Activity = UserData.Activity.Statuses.Offline;
		OnlineAt = e.OnlineAt;
		AppearedOffline?.Invoke(e: e);
	}

	internal async Task OnUpdatedPhoto(UpdatedPhotoEventArgs e)
	{
		if (!_photo.IsValueCreated)
			return;

		ProfilePhoto photo = await _photo;
		photo.UpdatePhoto(link: e.Link);
		UpdatedPhoto?.Invoke(e: e);
	}

	internal async Task OnDeletedPhoto(DeletedPhotoEventArgs e)
	{
		if (!_photo.IsValueCreated)
			return;

		ProfilePhoto photo = await _photo;
		photo.UpdatePhoto(link: null);
		DeletedPhoto?.Invoke(e: e);
	}
	#endregion
	#endregion
}