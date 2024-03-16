using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants;

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
	private record ChangePhoneResponse(string Phone, string Message);

	private record ChangeEmailRequest(string NewEmail);
	private record ChangeEmailResponse(string Email, string Message);
	#endregion

	#region Enums
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

	public sealed class InterlocutorOnlineEventArgs(int InterlocutorId, DateTime? OnlineAt) : EventArgs;
	public delegate void InterlocutorOnlineHandler(InterlocutorOnlineEventArgs e);
	public event InterlocutorOnlineHandler? OnInterlocutorOnline;

	public sealed class InterlocutorOfflineEventArgs(int InterlocutorId, DateTime? OnlineAt) : EventArgs;
	public delegate void InterlocutorOfflineHandler(InterlocutorOfflineEventArgs e);
	public event InterlocutorOfflineHandler? OnInterlocutorOffline;

	public sealed class InterlocutorUpdatedPhotoEventArgs(int InterlocutorId) : EventArgs;
	public delegate void InterlocutorUpdatedPhotoHandler(InterlocutorUpdatedPhotoEventArgs e);
	public event InterlocutorUpdatedPhotoHandler? OnInterlocutorUpdatedPhoto;

	public sealed class InterlocutorDeletedPhotoEventArgs(int InterlocutorId) : EventArgs;
	public delegate void InterlocutorDeletedPhotoHandler(InterlocutorDeletedPhotoEventArgs e);
	public event InterlocutorDeletedPhotoHandler? OnInterlocutorDeletedPhoto;

	public sealed class SignInEventArgs : EventArgs;
	public delegate void SignInHandler(SignInEventArgs e);
	public event SignInHandler? OnSignIn;

	public sealed class SignOutEventArgs(IEnumerable<int> SessionIds) : EventArgs;
	public delegate void SignOutHandler(SignOutEventArgs e);
	public event SignOutHandler? OnSignOut;

	public sealed class JoinedInChatEventArgs(string? ChatName) : EventArgs;
	public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
	public event JoinedInChatHandler? OnJoinedInChat;

	public sealed class ChangedPhoneEventArgs(string? Phone) : EventArgs;
	public delegate void ChangedPhoneHandler(ChangedPhoneEventArgs e);
	public event ChangedPhoneHandler? OnChangedPhone;

	public sealed class ChangedEmailEventArgs(string? Email) : EventArgs;
	public delegate void ChangedEmailHandler(ChangedEmailEventArgs e);
	public event ChangedEmailHandler? OnChangedEmail;

	public static async Task<User> Create(
		ApiClient client,
		int sessionId,
		string token,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		client.Token = token;
		UserInformationResponse response = await client.GetAsync<UserInformationResponse>(
			apiMethod: UserControllerMethods.GetInformation,
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
		createdUser._userHubConnection.On<int, DateTime?>(methodName: "SetOnline", handler: (userId, onlineAt) =>
		{
			InterlocutorOnlineEventArgs e = new InterlocutorOnlineEventArgs(InterlocutorId: userId, OnlineAt: onlineAt);
			createdUser.OnInterlocutorOnline?.Invoke(e: e);
		});
		createdUser._userHubConnection.On<int, DateTime?>(methodName: "SetOffline", handler: (userId, onlineAt) =>
		{
			InterlocutorOfflineEventArgs e = new InterlocutorOfflineEventArgs(InterlocutorId: userId, OnlineAt: onlineAt);
			createdUser.OnInterlocutorOffline?.Invoke(e: e);
		});
		createdUser._userHubConnection.On<int>(methodName: "UpdatedProfilePhoto", handler: userId =>
		{
			InterlocutorUpdatedPhotoEventArgs e = new InterlocutorUpdatedPhotoEventArgs(InterlocutorId: userId);
			createdUser.OnInterlocutorUpdatedPhoto?.Invoke(e: e);
		});
		createdUser._userHubConnection.On<int>(methodName: "DeletedProfilePhoto", handler: userId =>
		{
			InterlocutorDeletedPhotoEventArgs e = new InterlocutorDeletedPhotoEventArgs(InterlocutorId: userId);
			createdUser.OnInterlocutorDeletedPhoto?.Invoke(e: e);
		});
		createdUser._userHubConnection.On(methodName: "SignIn", handler: () =>
		{
			SignInEventArgs e = new SignInEventArgs();
			createdUser.OnSignIn?.Invoke(e: e);
		});
			createdUser._userHubConnection.On<IEnumerable<int>>(methodName: "SignOut", handler: (sessionIds) =>
		{
			SignOutEventArgs e = new SignOutEventArgs(SessionIds: sessionIds);
			createdUser.OnSignOut?.Invoke(e: e);
		});
		createdUser._userHubConnection.On<string?>(methodName: "JoinedInChat", handler: (chatName) =>
		{
			JoinedInChatEventArgs e = new JoinedInChatEventArgs(ChatName: chatName);
			createdUser.OnJoinedInChat?.Invoke(e: e);
		});
		createdUser._userHubConnection.On<string?>(methodName: "SetPhone", handler: (phone) =>
		{
			ChangedPhoneEventArgs e = new ChangedPhoneEventArgs(Phone: phone);
			createdUser.OnChangedPhone?.Invoke(e: e);
		});
		createdUser._userHubConnection.On<string?>(methodName: "SetEmail", handler: (email) =>
		{
			ChangedEmailEventArgs e = new ChangedEmailEventArgs(Email: email);
			createdUser.OnChangedEmail?.Invoke(e: e);
		});

		return createdUser;
	}

	private async Task<string> SignOut(
		string method,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		SignOutResponse response = await _client.PostAsync<SignOutResponse>(
			apiMethod: method,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}

	private async Task SetActivityStatus(
		string method,
		CancellationToken cancellationToken = default(CancellationToken)
	) => await _client.PutAsync(apiMethod: method, cancellationToken: cancellationToken);

	public async Task SetOffline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(method: UserControllerMethods.SetOffline, cancellationToken: cancellationToken);

	public async Task SetOnline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(method: UserControllerMethods.SetOnline, cancellationToken: cancellationToken);

	public async Task<string> SignOut(CancellationToken cancellationToken = default(CancellationToken))
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutThis, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> SignOutAll(CancellationToken cancellationToken = default(CancellationToken))
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutAll, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> SignOutAllExceptThis(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(method: AccountControllerMethods.SignOutOthers, cancellationToken: cancellationToken);

	public async Task UploadProfilePhoto(
		string pathToPhoto,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UploadProfilePhotoResponse? response = await _client.PutFileAsync<UploadProfilePhotoResponse>(
			apiMethod: UserControllerMethods.UploadProfilePhoto,
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
			apiMethod: UserControllerMethods.DownloadProfilePhoto,
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
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.DeleteAsync(apiMethod: UserControllerMethods.DeleteProfilePhoto, cancellationToken: cancellationToken);
		Photo = null;
	}

	public async Task ChangeEmail(
		string email,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		ChangeEmailResponse response = await _client.PutAsync<ChangeEmailResponse, ChangeEmailRequest>(
			apiMethod: UserControllerMethods.ChangeEmail,
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
			apiMethod: UserControllerMethods.ChangePhone,
			arg: new ChangePhoneRequest(NewPhone: phone),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Phone = response.Phone;
	}
}