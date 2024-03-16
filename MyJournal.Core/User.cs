using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Utilities;

namespace MyJournal.Core;

public sealed class User
{
	private readonly HubConnection _userHubConnection;
	private readonly ApiClient _client;

	private User(
		ApiClient client,
		HubConnection userHubConnection,
		int sessionId,
		string surname,
		string name,
		string? patronymic,
		string? phone = null,
		string? email = null,
		string? photo = null,
		ActivityStatus? activity = null,
		DateTime? onlineAt = null
	)
	{
		_client = client;
		_userHubConnection = userHubConnection;

		SessionId = sessionId;

		Surname = surname;
		Name = name;
		Patronymic = patronymic;
		Phone = phone;
		Email = email;
		Activity = activity;
		OnlineAt = onlineAt;
		Photo = photo;
	}

	#region Records

	private record SignOutResponse(string Message);

	private record UserInformationResponse(
		string Surname,
		string Name,
		string? Patronymic,
		string? Phone,
		string? Email,
		string? Photo
	);

	private record UploadProfilePhotoResponse(string Link);

	private record ChangePhoneRequest(string NewPhone);
	public record ChangePhoneResponse(string Phone, string Message);

	public record ChangeEmailRequest(string NewEmail);
	public record ChangeEmailResponse(string Email, string Message);
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

	private int SessionId { get; init; }
	public string Surname { get; init; }
	public string Name { get; init; }
	public string? Patronymic { get; init; }
	public string? Phone { get; private set; }
	public string? Email { get; private set; }
	public ActivityStatus? Activity { get; private set; }
	public DateTime? OnlineAt { get; private set; }
	public string? Photo { get; private set; }

	#endregion

	public delegate void SignedInUserHandler(int userId, DateTime? onlineAt);
	public event SignedInUserHandler? SignedInUser;

	public delegate void SignedOutUserHandler(int userId, DateTime? onlineAt);
	public event SignedOutUserHandler? SignedOutUser;

	public delegate void UpdatedProfilePhotoHandler(int userId);
	public event UpdatedProfilePhotoHandler? UpdatedProfilePhoto;

	public delegate void DeletedProfilePhotoHandler(int userId);
	public event DeletedProfilePhotoHandler? DeletedProfilePhoto;

	public static async Task<User> Create(
		ApiClient client,
		int sessionId,
		string token,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		client.Token = token;
		UserInformationResponse response = await client.GetAsync<UserInformationResponse>(
			apiMethod: "user/profile/info/me",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		User createdUser = new User(
			client: client,
			userHubConnection: DefaultHubConnectionBuilder.CreateHubConnection(
				url: "https://localhost:7267/hub/User",
				token: client.Token
			),
			sessionId: sessionId,
			surname: response.Surname,
			name: response.Name,
			patronymic: response.Patronymic,
			phone: response.Phone,
			email: response.Email,
			photo: response.Photo
		);

		await createdUser._userHubConnection.StartAsync(cancellationToken: cancellationToken);
		createdUser._userHubConnection.On<int, DateTime?>(
			methodName: "SetOnline",
			handler: (userId, onlineAt) => createdUser.SignedInUser?.Invoke(userId: userId, onlineAt: onlineAt)
		);
		createdUser._userHubConnection.On<int, DateTime?>(
			methodName: "SetOffline",
			handler: (userId, onlineAt) => createdUser.SignedOutUser?.Invoke(userId: userId, onlineAt: onlineAt)
		);
		createdUser._userHubConnection.On<int>(
			methodName: "UpdatedProfilePhoto",
			handler: userId => createdUser.UpdatedProfilePhoto?.Invoke(userId: userId)
		);
		createdUser._userHubConnection.On<int>(
			methodName: "DeletedProfilePhoto",
			handler: userId => createdUser.DeletedProfilePhoto?.Invoke(userId: userId)
		);

		return createdUser;
	}

	private async Task<string> SignOut(
		SignOutOptions options,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		SignOutResponse response = await _client.PostAsync<SignOutResponse>(
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
		await _client.PutAsync(
			apiMethod: $"user/profile/activity/{activity.ToString().ToLower()}",
			cancellationToken: cancellationToken
		);
	}

	public async Task SetOffline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(activity: ActivityStatus.Offline, cancellationToken: cancellationToken);

	public async Task SetOnline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(activity: ActivityStatus.Online, cancellationToken: cancellationToken);

	public async Task<string> SignOut(CancellationToken cancellationToken = default(CancellationToken))
	{
		string message = await SignOut(options: SignOutOptions.This, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> SignOutAll(CancellationToken cancellationToken = default(CancellationToken))
	{
		string message = await SignOut(options: SignOutOptions.All, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> SignOutAllExceptThis(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.Others, cancellationToken: cancellationToken);

	public async Task UploadProfilePhoto(
		string pathToPhoto,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UploadProfilePhotoResponse? response = await _client.PutFileAsync<UploadProfilePhotoResponse>(
			apiMethod: "user/profile/photo/upload",
			path: pathToPhoto,
			cancellationToken: cancellationToken
		);
		Photo = response?.Link;
	}

	public async Task DownloadProfilePhoto(
		string folderToSave,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		byte[] response = await _client.GetBytesAsync(
			apiMethod: "user/profile/photo/download",
			cancellationToken: cancellationToken
		);
		string fileExtension = _client.ContentType.Split(separator: '/').Last();
		await File.WriteAllBytesAsync(
			path: Path.Join(path1: folderToSave, path2: $"{Surname} {Name} {Patronymic}.{fileExtension}"),
			bytes: response,
			cancellationToken: cancellationToken
		);
	}

	public async Task DeleteProfilePhoto(
		string pathToSave,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.DeleteAsync(apiMethod: "user/profile/photo/delete", cancellationToken: cancellationToken);
		Photo = null;
	}

	public async Task ChangeEmail(
		string email,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		ChangeEmailResponse response = await _client.PutAsync<ChangeEmailResponse, ChangeEmailRequest>(
			apiMethod: "user/profile/security/email/change",
			arg: new ChangeEmailRequest(NewEmail: email),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Email = response.Email;
	}

	public async Task ChangePhone(
		string phone,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		ChangePhoneResponse response = await _client.PutAsync<ChangePhoneResponse, ChangePhoneRequest>(
			apiMethod: "user/profile/security/phone/change",
			arg: new ChangePhoneRequest(NewPhone: phone),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Phone = response.Phone;
	}
}