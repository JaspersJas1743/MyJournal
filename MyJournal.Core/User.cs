using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Utilities;

namespace MyJournal.Core;

public sealed class User
{
	private HubConnection _userHubConnection;

	private User(string surname, string name, string? patronymic, string? phone = null, string? email = null, string? activity = null, DateTime? onlineAt = null)
	{
		Surname = surname;
		Name = name;
		Patronymic = patronymic;
		Phone = phone;
		Email = email;
		Activity = activity is null ? null : Enum.Parse<ActivityStatus>(value: activity);
		OnlineAt = onlineAt;
	}

	#region Records
	private record SignOutResponse(string Message);
	private record UserInformation(string Surname, string Name, string? Patronymic, string? Phone, string? Email);
	private record UserInformationResponse(string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	public record UploadProfilePhotoResponse(string Link);
	#endregion

	#region Enums
	private enum SignOutOptions
	{
		This,
		All,
		Others
	}

	public enum ActivityStatus
	{
		Online,
		Offline
	}
	#endregion

	#region Properties
	public string Surname { get; init; }
	public string Name { get; init; }
	public string? Patronymic { get; init; }
	public string? Phone { get; init; }
	public string? Email { get; init; }
	public ActivityStatus? Activity { get; init; }
	public DateTime? OnlineAt { get; init; }
	#endregion

	public delegate void SignedInUserHandler(int userId, DateTime? onlineAt);
	public event SignedInUserHandler? SignedInUser;
	public delegate void SignedOutUserHandler(int userId, DateTime? onlineAt);
	public event SignedOutUserHandler? SignedOutUser;
	public delegate void UpdatedProfilePhotoHandler(int userId);
	public event UpdatedProfilePhotoHandler? UpdatedProfilePhoto;
	public delegate void DeletedProfilePhotoHandler(int userId);
	public event DeletedProfilePhotoHandler? DeletedProfilePhoto;

	public static async Task<User> Create(string token, CancellationToken cancellationToken = default(CancellationToken))
	{
		ApiClient.Token = token;
		UserInformation response = await ApiClient.GetAsync<UserInformation>(
			apiMethod: "user/profile/info/me",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		User createdUser = new User(
			surname: response.Surname,
			name: response.Name,
			patronymic: response.Patronymic,
			phone: response.Phone,
			email: response.Email
		);

		createdUser._userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(url: "https://localhost:7267/hub/User");

		await createdUser._userHubConnection.StartAsync(cancellationToken: cancellationToken);
		createdUser._userHubConnection.On<int, DateTime?>(methodName: "SetOnline",
			handler: (userId, onlineAt) => createdUser.SignedInUser?.Invoke(userId: userId, onlineAt: onlineAt)
		);
		createdUser._userHubConnection.On<int, DateTime?>(methodName: "SetOffline",
			handler: (userId, onlineAt) => createdUser.SignedOutUser?.Invoke(userId: userId, onlineAt: onlineAt)
		);
		createdUser._userHubConnection.On<int>(methodName: "UpdatedProfilePhoto",
			handler: userId => createdUser.UpdatedProfilePhoto?.Invoke(userId: userId)
		);
		createdUser._userHubConnection.On<int>(methodName: "DeletedProfilePhoto",
			handler: userId => createdUser.DeletedProfilePhoto?.Invoke(userId: userId)
		);

		return createdUser;
	}

	private async Task<string> SignOut(SignOutOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		SignOutResponse response = await ApiClient.PostAsync<SignOutResponse>(
			apiMethod: $"account/sign-out/{options.ToString().ToLower()}",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.Message;
	}

	private async Task SetActivityStatus(
		ActivityStatus activity,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await ApiClient.PutAsync(
			apiMethod: $"profile/activity/{activity.ToString().ToLower()}",
			cancellationToken: cancellationToken
		);
	}

	public async Task SetOffline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(activity: ActivityStatus.Offline, cancellationToken: cancellationToken);

	public async Task SetOnline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(activity: ActivityStatus.Online, cancellationToken: cancellationToken);

	public async Task<string> SignOut(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.This, cancellationToken: cancellationToken);

	public async Task<string> SignOutAll(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.All, cancellationToken: cancellationToken);

	public async Task<string> SignOutAllExceptThis(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.Others, cancellationToken: cancellationToken);

	public async Task UploadProfilePhoto(string pathToPhoto, CancellationToken cancellationToken = default(CancellationToken))
	{
		UploadProfilePhotoResponse? response = await _client.PutFileAsync<UploadProfilePhotoResponse>(
			apiMethod: "user/profile/photo/upload",
			path: pathToPhoto,
			cancellationToken: cancellationToken
		);
		Photo = response?.Link;
	}

	public async Task DownloadProfilePhoto(string folderToSave, CancellationToken cancellationToken = default(CancellationToken))
	{
		byte[] response = await _client.GetBytesAsync(apiMethod: "user/profile/photo/download", cancellationToken: cancellationToken);
		string fileExtension = _client.ContentType.Split(separator: '/').Last();
		await File.WriteAllBytesAsync(
			path: Path.Join(path1: folderToSave, path2: $"{Surname} {Name} {Patronymic}.{fileExtension}"),
			bytes: response, cancellationToken: cancellationToken
		);
	}

	public async Task DeleteProfilePhoto(string pathToSave, CancellationToken cancellationToken = default(CancellationToken))
	{
		await _client.DeleteAsync(apiMethod: "user/profile/photo/delete", cancellationToken: cancellationToken);
		Photo = null;
	}
}