using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.UserData;

public sealed class ProfilePhoto(
	ApiClient client,
	string? link
)
{
	#region Properties
	public string? Link { get; private set; } = link;
	#endregion

	#region Records
	private sealed record UploadProfilePhotoResponse(string Link);
	#endregion

	#region Methods
	public async Task Update(
		string pathToPhoto,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UploadProfilePhotoResponse? response = await client.PutFileAsync<UploadProfilePhotoResponse>(
			apiMethod: UserControllerMethods.UploadProfilePhoto,
			path: pathToPhoto,
			cancellationToken: cancellationToken
		);
		Link = response?.Link;
	}

	public async Task Delete(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await client.DeleteAsync(apiMethod: UserControllerMethods.DeleteProfilePhoto, cancellationToken: cancellationToken);
		Link = null;
	}

	public async Task Download(
		string folder,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		byte[] response = await client.GetBytesAsync(
			apiMethod: UserControllerMethods.DownloadProfilePhoto,
			cancellationToken: cancellationToken
		);
		await File.WriteAllBytesAsync(
			path: Path.Combine(path1: folder, path2: client.FileName),
			bytes: response,
			cancellationToken: cancellationToken
		);
	}
	#endregion
}