using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Chats;
using MyJournal.Core.Interlocutors;
using MyJournal.Core.UserData;
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
		client.ClientId = information.Id;
		_client = client;
		_googleAuthenticatorService = googleAuthenticatorService;
		_sessionId = client.SessionId;
		_userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: UserHubMethods.HubEndpoint,
			token: client.Token!
		);

		PersonalData = new PersonalData()
		{
			Surname = information.Surname,
			Name = information.Name,
			Patronymic = information.Patronymic
		};
		Chats = chats;
		Interlocutors = interlocutors;
		Security = new Security(client: client)
		{
			Email = new Email(client: client, googleAuthenticatorService: googleAuthenticatorService, email: information.Email),
			Password = new Password(client: client, googleAuthenticatorService: googleAuthenticatorService),
			Phone = new Phone(client: client, googleAuthenticatorService: googleAuthenticatorService, phone: information.Phone)
		};
		Photo = new ProfilePhoto(client: client, link: information.Photo);
		Activity = new Activity(client: client);
	}

	~User() => _client.Dispose();
	#endregion

	#region Properties
	public PersonalData PersonalData { get; }
	public ChatCollection Chats { get; }
	public InterlocutorCollection Interlocutors { get; }
	public Security Security { get; }
	public ProfilePhoto Photo { get; }
	public Activity Activity { get; }
	#endregion

	#region Records
	protected sealed record UserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	#endregion

	#region Classes
	public sealed class InterlocutorOnlineEventArgs(int interlocutorId, DateTime? onlineAt) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
		public DateTime? OnlineAt { get; } = onlineAt;
	}

	public sealed class InterlocutorOfflineEventArgs(int interlocutorId, DateTime? onlineAt) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
		public DateTime? OnlineAt { get; } = onlineAt;
	}

	public sealed class InterlocutorUpdatedPhotoEventArgs(int interlocutorId) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
	}

	public sealed class InterlocutorDeletedPhotoEventArgs(int interlocutorId) : EventArgs
    {
		public int InterlocutorId { get; } = interlocutorId;
	}

	public sealed class SignedInEventArgs : EventArgs;

	public sealed class SignedOutEventArgs(IEnumerable<int> sessionIds, bool currentSessionAreClosed) : EventArgs
	{
		public IEnumerable<int> SessionIds { get; } = sessionIds;
		public bool CurrentSessionAreClosed { get; } = currentSessionAreClosed;
	}

	public sealed class JoinedInChatEventArgs(int chatId) : EventArgs
	{
		public int ChatId { get; } = chatId;
	}
	#endregion

	#region Delegates
	public delegate void InterlocutorOnlineHandler(InterlocutorOnlineEventArgs e);
	public delegate void InterlocutorOfflineHandler(InterlocutorOfflineEventArgs e);
	public delegate void InterlocutorUpdatedPhotoHandler(InterlocutorUpdatedPhotoEventArgs e);
	public delegate void InterlocutorDeletedPhotoHandler(InterlocutorDeletedPhotoEventArgs e);
	public delegate void SignedInHandler(SignedInEventArgs e);
	public delegate void SignedOutHandler(SignedOutEventArgs e);
	public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
	#endregion

	#region Events
	public event InterlocutorOnlineHandler? InterlocutorAppearedOnline;
	public event InterlocutorOfflineHandler? InterlocutorAppearedOffline;
	public event InterlocutorUpdatedPhotoHandler? InterlocutorUpdatedPhoto;
	public event InterlocutorDeletedPhotoHandler? InterlocutorDeletedPhoto;
	public event SignedInHandler? SignedIn;
	public event SignedOutHandler? SignedOut;
	public event JoinedInChatHandler? JoinedInChat;
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
			InterlocutorAppearedOnline?.Invoke(e: new InterlocutorOnlineEventArgs(
				interlocutorId: userId,
				onlineAt: onlineAt
			))
		);
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOffline, handler: (userId, onlineAt) =>
			InterlocutorAppearedOffline?.Invoke(e: new InterlocutorOfflineEventArgs(
				interlocutorId: userId,
				onlineAt: onlineAt
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.UpdatedProfilePhoto, handler: userId =>
			InterlocutorUpdatedPhoto?.Invoke(e: new InterlocutorUpdatedPhotoEventArgs(
				interlocutorId: userId
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.DeletedProfilePhoto, handler: userId =>
			InterlocutorDeletedPhoto?.Invoke(e: new InterlocutorDeletedPhotoEventArgs(
				interlocutorId: userId
			))
		);
		_userHubConnection.On(methodName: UserHubMethods.SignIn, handler: () =>
			SignedIn?.Invoke(e: new SignedInEventArgs())
		);
		_userHubConnection.On<IEnumerable<int>>(methodName: UserHubMethods.SignOut, handler: (sessionIds) =>
			SignedOut?.Invoke(e: new SignedOutEventArgs(
				sessionIds: sessionIds,
				currentSessionAreClosed: sessionIds.Contains(value: _sessionId)
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.JoinedInChat, handler: async (chatId) =>
		{
			await Chats.Append(chatId: chatId, cancellationToken: cancellationToken);
			JoinedInChat?.Invoke(e: new JoinedInChatEventArgs(chatId: chatId));
		});
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetPhone, handler: (phone) =>
			Security.Phone?.OnUpdated(e: new Phone.UpdatedPhoneEventArgs(phone: phone))
		);
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetEmail, handler: (email) =>
			Security.Email?.OnUpdated(e: new Email.UpdatedEmailEventArgs(email: email))
		);
	}
	#endregion
}