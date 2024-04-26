using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.UserData;

public sealed class ProfilePhoto(
	ApiClient client,
	IFileService fileService,
	string? link
)
{
	#region Fields
	private const string DefaultBucket = "profile_photos";
	#endregion

	#region Properties
	public string? Link { get; private set; } = link;
	#endregion

	#region Events
	public event UpdatedProfilePhotoHandler UpdatedProfilePhoto;
	#endregion

	#region Records
	private sealed record UploadProfilePhotoRequest(string? Link);
	private sealed record UploadProfilePhotoResponse(string Message);
	#endregion

	#region Methods
	public async Task Update(
		string pathToPhoto,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (Link is not null)
			await fileService.Delete(link: Link, cancellationToken: cancellationToken);
		
		Link = await fileService.Upload(folderToSave: DefaultBucket, pathToFile: pathToPhoto, cancellationToken: cancellationToken);
		UpdatedProfilePhoto?.Invoke(e: new UpdatedProfilePhotoEventArgs(link: Link));

		_ = await client.PutAsync<UploadProfilePhotoResponse>(
			uri: ApiClient.CreateUri(
				apiMethod: UserControllerMethods.UploadProfilePhoto,
				arg: new UploadProfilePhotoRequest(Link: Link)
			), cancellationToken: cancellationToken
		);
	}

	public async Task Delete(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (Link is null)
			throw new ArgumentNullException(message: "Фотография пользователя отсутствует.", paramName: nameof(Link));

		await fileService.Delete(link: Link, cancellationToken: cancellationToken);
		await client.DeleteAsync(apiMethod: UserControllerMethods.DeleteProfilePhoto, cancellationToken: cancellationToken);
		Link = null;
		UpdatedProfilePhoto?.Invoke(e: new UpdatedProfilePhotoEventArgs(link: Link));
	}

	public async Task Download(
		string folder,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (Link is null)
			throw new ArgumentNullException(message: "Фотография пользователя отсутствует.", paramName: nameof(Link));

		await fileService.Download(link: Link, pathToSave: folder, cancellationToken: cancellationToken);
	}

	internal void UpdatePhoto(string? link)
		=> Link = link;
	#endregion
}