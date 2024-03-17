using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public class User
{
	#region Fields
	private readonly ApiClient _client;
	private readonly HubConnection _userHubConnection;
	private readonly IGoogleAuthenticatorService _googleAuthenticatorService;
	private readonly int _id;
	private readonly int _sessionId;
	#endregion

	#region Constructors
	protected User(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		ChatCollection chats,
		InterlocutorCollection interlocutors
	)
	{
		_client = client;
		_googleAuthenticatorService = googleAuthenticatorService;
		_userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: UserHubMethods.HubEndpoint,
			token: client.Token!
		);

		_id = information.Id;
		_sessionId = client.SessionId;

		Surname = information.Surname;
		Name = information.Name;
		Patronymic = information.Patronymic;
		Phone = information.Phone;
		Email = information.Email;
		Photo = information.Photo;

		Chats = chats;
		Interlocutors = interlocutors;
	}

	~User() => _client.Dispose();
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
	protected sealed record UserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);

	private sealed record SignOutResponse(string Message);
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
	protected static async Task<UserInformationResponse> GetUserInformation(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await client.GetAsync<UserInformationResponse>(
			apiMethod: UserControllerMethods.GetInformation,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
	}

	protected async Task ConnectToUserHub(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _userHubConnection.StartAsync(cancellationToken: cancellationToken);
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOnline, handler: (userId, onlineAt) =>
			OnInterlocutorOnline?.Invoke(e: new InterlocutorOnlineEventArgs(
				InterlocutorId: userId,
				OnlineAt: onlineAt
			))
		);
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOffline, handler: (userId, onlineAt) =>
			OnInterlocutorOffline?.Invoke(e: new InterlocutorOfflineEventArgs(
				InterlocutorId: userId,
				OnlineAt: onlineAt
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.UpdatedProfilePhoto, handler: userId =>
			OnInterlocutorUpdatedPhoto?.Invoke(e: new InterlocutorUpdatedPhotoEventArgs(
				InterlocutorId: userId
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.DeletedProfilePhoto, handler: userId =>
			OnInterlocutorDeletedPhoto?.Invoke(e: new InterlocutorDeletedPhotoEventArgs(
				InterlocutorId: userId
			))
		);
		_userHubConnection.On(methodName: UserHubMethods.SignIn, handler: () =>
			OnSignedIn?.Invoke(e: new SignedInEventArgs())
		);
		_userHubConnection.On<IEnumerable<int>>(methodName: UserHubMethods.SignOut, handler: (sessionIds) =>
			OnSignedOut?.Invoke(e: new SignedOutEventArgs(
				SessionIds: sessionIds,
				CurrentSessionAreClosed: sessionIds.Contains(value: _sessionId)
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.JoinedInChat, handler: async (chatId) =>
		{
			await Chats.Append(chatId: chatId, cancellationToken: cancellationToken);
			OnJoinedInChat?.Invoke(e: new JoinedInChatEventArgs(ChatId: chatId));
		});
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetPhone, handler: (phone) =>
			OnUpdatedPhone?.Invoke(e: new UpdatedPhoneEventArgs(Phone: phone))
		);
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetEmail, handler: (email) =>
			OnUpdatedEmail?.Invoke(e: new UpdatedEmailEventArgs(Email: email))
		);
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