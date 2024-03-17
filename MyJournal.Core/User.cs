using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public class User
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IGoogleAuthenticatorService _googleAuthenticatorService;
	private readonly HubConnection _userHubConnection;
	private readonly int _id;
	private readonly int _sessionId;
	#endregion

	#region Constructors
	private User(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		HubConnection userHubConnection,
		int id,
		int sessionId,
		string surname,
		string name,
		string? patronymic,
		ChatCollection chats,
		InterlocutorCollection interlocutors,
		string? phone = null,
		string? email = null,
		string? photo = null
	)
	{
		_client = client;
		_googleAuthenticatorService = googleAuthenticatorService;
		_userHubConnection = userHubConnection;

		_id = id;
		_sessionId = sessionId;

		Surname = surname;
		Name = name;
		Patronymic = patronymic;
		Chats = chats;
		Interlocutors = interlocutors;

		Phone = phone;
		Email = email;
		Photo = photo;
	}
	#endregion

	#region Properties
	public string Surname { get; }
	public string Name { get; }
	public string? Patronymic { get; }
	public ChatCollection Chats { get; }
	public InterlocutorCollection Interlocutors { get; }
	public string? Phone { get; private set; }
	public string? Email { get; private set; }
	public string? Photo { get; private set; }
	#endregion

	#region Records
	private sealed record SignOutResponse(string Message);
	private sealed record UserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	private sealed record UploadProfilePhotoResponse(string Link);
	private sealed record ChangePhoneRequest(string NewPhone);
	private sealed record ChangePhoneResponse(string Phone, string Message);
	private sealed record ChangeEmailRequest(string NewEmail);
	private sealed record ChangeEmailResponse(string Email, string Message);
	private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
	private sealed record ChangePasswordResponse(string Message);
	#endregion

	#region Classes
	public sealed class InterlocutorOnlineEventArgs(int InterlocutorId, DateTime? OnlineAt) : EventArgs;
	public sealed class InterlocutorOfflineEventArgs(int InterlocutorId, DateTime? OnlineAt) : EventArgs;
	public sealed class InterlocutorUpdatedPhotoEventArgs(int InterlocutorId) : EventArgs;
	public sealed class InterlocutorDeletedPhotoEventArgs(int InterlocutorId) : EventArgs;
	public sealed class SignedInEventArgs : EventArgs;
	public sealed class SignedOutEventArgs(IEnumerable<int> SessionIds, bool CurrentSessionAreClosed) : EventArgs;
	public sealed class JoinedInChatEventArgs(int ChatId) : EventArgs;
	public sealed class UpdatedPhoneEventArgs(string? Phone) : EventArgs;
	public sealed class UpdatedEmailEventArgs(string? Email) : EventArgs;
	#endregion

	#region Delegates
	public delegate void InterlocutorOnlineHandler(InterlocutorOnlineEventArgs e);
	public delegate void InterlocutorOfflineHandler(InterlocutorOfflineEventArgs e);
	public delegate void InterlocutorUpdatedPhotoHandler(InterlocutorUpdatedPhotoEventArgs e);
	public delegate void InterlocutorDeletedPhotoHandler(InterlocutorDeletedPhotoEventArgs e);
	public delegate void SignedInHandler(SignedInEventArgs e);
	public delegate void SignedOutHandler(SignedOutEventArgs e);
	public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
	public delegate void UpdatedPhoneHandler(UpdatedPhoneEventArgs e);
	public delegate void UpdatedEmailHandler(UpdatedEmailEventArgs e);
	#endregion

	#region Events
	public event InterlocutorOnlineHandler? OnInterlocutorOnline;
	public event InterlocutorOfflineHandler? OnInterlocutorOffline;
	public event InterlocutorUpdatedPhotoHandler? OnInterlocutorUpdatedPhoto;
	public event InterlocutorDeletedPhotoHandler? OnInterlocutorDeletedPhoto;
	public event SignedInHandler? OnSignedIn;
	public event SignedOutHandler? OnSignedOut;
	public event JoinedInChatHandler? OnJoinedInChat;
	public event UpdatedPhoneHandler? OnUpdatedPhone;
	public event UpdatedEmailHandler? OnUpdatedEmail;
	#endregion

	#region Methods
	public static async Task<User> Create(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
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
			googleAuthenticatorService: googleAuthenticatorService,
			userHubConnection: DefaultHubConnectionBuilder.CreateHubConnection(
				url: "https://localhost:7267/hub/User",
				token: client.Token
			),
			id: response.Id,
			sessionId: sessionId,
			surname: response.Surname,
			name: response.Name,
			patronymic: response.Patronymic,
			chats: await ChatCollection.Create(client: client, cancellationToken: cancellationToken),
			interlocutors: await InterlocutorCollection.Create(client: client, cancellationToken: cancellationToken),
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
			await createdUser.Chats.Append(chatId: chatId, cancellationToken: cancellationToken);
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

	public async Task<UserInformation> GetInformationAbout(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformation response = await _client.GetAsync<UserInformation>(
			apiMethod: UserControllerMethods.GetInformationAbout(userId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response;

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

	public async Task SetOffline(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SetActivityStatus(method: UserControllerMethods.SetOffline, cancellationToken: cancellationToken);

	public async Task SetOnline(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SetActivityStatus(method: UserControllerMethods.SetOnline, cancellationToken: cancellationToken);

	public async Task<string> SignOut(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutThis, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> SignOutAll(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutAll, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> SignOutOthers(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SignOut(method: AccountControllerMethods.SignOutOthers, cancellationToken: cancellationToken);

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

	public async Task<string> ChangeEmail(
		string code,
		string email,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		bool isVerified = await _googleAuthenticatorService.VerifyAuthenticationCode(userId: _id, code: code, cancellationToken: cancellationToken);
		if (!isVerified)
			throw new ArgumentException(message: "Некорректный код подтверждения для смены адреса электронной почты.", paramName: nameof(code));

		ChangeEmailResponse response = await _client.PutAsync<ChangeEmailResponse, ChangeEmailRequest>(
			apiMethod: UserControllerMethods.ChangeEmail,
			arg: new ChangeEmailRequest(NewEmail: email),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Email = response.Email;
		return response.Message;
	}

	public async Task<string> ChangePhone(
		string code,
		string phone,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		bool isVerified = await _googleAuthenticatorService.VerifyAuthenticationCode(userId: _id, code: code, cancellationToken: cancellationToken);
		if (!isVerified)
			throw new ArgumentException(message: "Некорректный код подтверждения для смены номера телефона.", paramName: nameof(code));

		ChangePhoneResponse response = await _client.PutAsync<ChangePhoneResponse, ChangePhoneRequest>(
			apiMethod: UserControllerMethods.ChangePhone,
			arg: new ChangePhoneRequest(NewPhone: phone),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Phone = response.Phone;
		return response.Message;
	}

	public async Task<string> ChangePassword(
		string code,
		string currentPassword,
		string newPassword,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		bool isVerified = await _googleAuthenticatorService.VerifyAuthenticationCode(userId: _id, code: code, cancellationToken: cancellationToken);
		if (!isVerified)
			throw new ArgumentException(message: "Некорректный код подтверждения для смены пароля.", paramName: nameof(code));

		ChangePasswordResponse response = await _client.PutAsync<ChangePasswordResponse, ChangePasswordRequest>(
			apiMethod: UserControllerMethods.ChangePassword,
			arg: new ChangePasswordRequest(CurrentPassword: currentPassword, NewPassword: newPassword),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}
	#endregion
}