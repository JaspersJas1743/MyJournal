using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;
using Activity = MyJournal.Core.UserData.Activity;

namespace MyJournal.Core;

public class User
{
	#region Fields
	private readonly ApiClient _client;
	private readonly HubConnection _userHubConnection;
	#endregion

	#region Constructors
	protected User(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		IFileService fileService,
		UserInformationResponse information,
		ChatCollection chats,
		InterlocutorCollection interlocutors,
		IntendedInterlocutorCollection intendedInterlocutors
	)
	{
		client.ClientId = information.Id;
		fileService.ApiClient = client;
		_client = client;
		_userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: UserHubMethods.HubEndpoint,
			token: client.Token!
		);

		PersonalData = new PersonalData(
			surname: information.Surname,
			name: information.Name,
			patronymic: information.Patronymic
		);
		Chats = chats;
		Interlocutors = interlocutors;
		IntendedInterlocutors = intendedInterlocutors;
		Security = new Security(
			client: client,
			phone: new Phone(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService,
				phone: information.Phone
			),
			email: new Email(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService,
				email: information.Email
			),
			password: new Password(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService
			)
		);
		Photo = new ProfilePhoto(
			client: client,
			fileService: fileService,
			link: information.Photo
		);
		Activity = new Activity(client: client);
	}

	~User() => _client.Dispose();
	#endregion

	#region Properties
	public PersonalData PersonalData { get; }
	public ChatCollection Chats { get; }
	public InterlocutorCollection Interlocutors { get; }
	public IntendedInterlocutorCollection IntendedInterlocutors { get; }
	public Security Security { get; }
	public ProfilePhoto Photo { get; }
	public Activity Activity { get; }
	#endregion

	#region Records
	protected sealed record UserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	#endregion

	#region Classes
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
	public delegate void SignedInHandler(SignedInEventArgs e);
	public delegate void SignedOutHandler(SignedOutEventArgs e);
	public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
	#endregion

	#region Events
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
			Interlocutors.OnAppearedOnline(e: new InterlocutorCollection.InterlocutorAppearedOnlineEventArgs(
				interlocutorId: userId,
				onlineAt: onlineAt
			))
		);
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOffline, handler: (userId, onlineAt) =>
			Interlocutors.OnAppearedOffline(e: new InterlocutorCollection.InterlocutorAppearedOfflineEventArgs(
				interlocutorId: userId,
				onlineAt: onlineAt
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.UpdatedProfilePhoto, handler: userId =>
			Interlocutors.OnUpdatedPhoto(e: new InterlocutorCollection.InterlocutorUpdatedPhotoEventArgs(
				interlocutorId: userId
			))
		);
		_userHubConnection.On<int>(methodName: UserHubMethods.DeletedProfilePhoto, handler: userId =>
			Interlocutors.OnDeletedPhoto(e: new InterlocutorCollection.InterlocutorDeletedPhotoEventArgs(
				interlocutorId: userId
			))
		);
		_userHubConnection.On(methodName: UserHubMethods.SignIn, handler: () =>
			SignedIn?.Invoke(e: new SignedInEventArgs())
		);
		_userHubConnection.On<IEnumerable<int>>(methodName: UserHubMethods.SignOut, handler: (sessionIds) =>
			SignedOut?.Invoke(e: new SignedOutEventArgs(
				sessionIds: sessionIds,
				currentSessionAreClosed: sessionIds.Contains(value: _client.SessionId)
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