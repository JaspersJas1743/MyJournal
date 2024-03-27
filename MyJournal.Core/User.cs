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

	private readonly Lazy<PersonalData> _personalData;
	private readonly Lazy<ChatCollection> _chats;
	private readonly Lazy<InterlocutorCollection> _interlocutors;
	private readonly Lazy<IntendedInterlocutorCollection> _intendedInterlocutors;
	private readonly Lazy<Security> _security;
	private readonly Lazy<ProfilePhoto> _photo;
	private readonly Lazy<Activity> _activity;
	#endregion

	#region Constructors
	protected User(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		IFileService fileService,
		UserInformationResponse information,
		Lazy<ChatCollection> chats,
		Lazy<InterlocutorCollection> interlocutors,
		Lazy<IntendedInterlocutorCollection> intendedInterlocutors,
		Lazy<SessionCollection> sessions
	)
	{
		client.ClientId = information.Id;
		fileService.ApiClient = client;
		_client = client;
		_userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: UserHubMethods.HubEndpoint,
			token: client.Token!
		);

		_personalData = new Lazy<PersonalData>(value: new PersonalData(
			surname: information.Surname,
			name: information.Name,
			patronymic: information.Patronymic
		));

		_chats = chats;
		_interlocutors = interlocutors;
		_intendedInterlocutors = intendedInterlocutors;
		_security = new Lazy<Security>(value: new Security(
			phone: new Lazy<Phone>(value: new Phone(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService,
				phone: information.Phone
			)),
			email: new Lazy<Email>(value: new Email(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService,
				email: information.Email
			)),
			password: new Lazy<Password>(value: new Password(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService
			)),
			sessions: sessions
		));
		_photo = new Lazy<ProfilePhoto>(value: new ProfilePhoto(
			client: client,
			fileService: fileService,
			link: information.Photo
		));
		_activity = new Lazy<Activity>(value: new Activity(
			client: client
		));
	}

	~User() => _client.Dispose();
	#endregion

	#region Properties
	public PersonalData PersonalData => _personalData.Value;
	public ChatCollection Chats => _chats.Value;
	public InterlocutorCollection Interlocutors => _interlocutors.Value;
	public IntendedInterlocutorCollection IntendedInterlocutors => _intendedInterlocutors.Value;
	public Security Security => _security.Value;
	public bool SecurityIsLoaded => _security.IsValueCreated;
	public ProfilePhoto Photo => _photo.Value;
	public bool PersonalDataIsLoaded => _personalData.IsValueCreated;
	public bool PhotoIsLoaded => _photo.IsValueCreated;
	public Activity Activity => _activity.Value;
	public bool ActivityIsLoaded => _activity.IsValueCreated;
	#endregion

	#region Records
	protected sealed record UserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	#endregion

	#region Classes
	public sealed class JoinedInChatEventArgs(int chatId) : EventArgs
	{
		public int ChatId { get; } = chatId;
	}
	#endregion

	#region Delegates
	public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
	#endregion

	#region Events
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
		{
			if (Interlocutors.IsLoaded)
			{
				Interlocutors.OnAppearedOnline(e: new InterlocutorCollection.InterlocutorAppearedOnlineEventArgs(
					   interlocutorId: userId,
					   onlineAt: onlineAt
				));
			}
		});
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOffline, handler: (userId, onlineAt) =>
		{
			if (Interlocutors.IsLoaded)
			{
				Interlocutors.OnAppearedOffline(e: new InterlocutorCollection.InterlocutorAppearedOfflineEventArgs(
					interlocutorId: userId,
					onlineAt: onlineAt
				));
			}
		});
		_userHubConnection.On<int, string>(methodName: UserHubMethods.UpdatedProfilePhoto, handler: (userId, link) =>
		{
			if (Interlocutors.IsLoaded)
			{
				Interlocutors.OnUpdatedPhoto(e: new InterlocutorCollection.InterlocutorUpdatedPhotoEventArgs(
					 interlocutorId: userId,
					 link: link
				));
			}
		});
		_userHubConnection.On<int>(methodName: UserHubMethods.DeletedProfilePhoto, handler: userId =>
		{
			if (Interlocutors.IsLoaded)
			{
				Interlocutors.OnDeletedPhoto(e: new InterlocutorCollection.InterlocutorDeletedPhotoEventArgs(
					 interlocutorId: userId
				));
			}
		});
		_userHubConnection.On<int>(methodName: UserHubMethods.SignIn, handler: async sessionId =>
		{
			if (SecurityIsLoaded)
			{
				await Security.Sessions.OnCreatedSession(
					e: new SessionCollection.CreatedSessionEventArgs(sessionId: sessionId),
					cancellationToken: cancellationToken
				);
			}
		});
		_userHubConnection.On<IEnumerable<int>>(methodName: UserHubMethods.SignOut, handler: async (sessionIds) =>
		{
			if (SecurityIsLoaded)
			{
				await Security.Sessions.OnClosedSession(e: new SessionCollection.ClosedSessionEventArgs(
					sessionIds: sessionIds,
					currentSessionAreClosed: sessionIds.Contains(value: _client.SessionId)
				), cancellationToken: cancellationToken);
			}
		});
		_userHubConnection.On<int>(methodName: UserHubMethods.JoinedInChat, handler: async (chatId) =>
		{
			if (Chats.IsLoaded)
				await Chats.Append(chatId: chatId, cancellationToken: cancellationToken);
			JoinedInChat?.Invoke(e: new JoinedInChatEventArgs(chatId: chatId));
		});
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetPhone, handler: (phone) =>
		{
			if (SecurityIsLoaded)
				Security.Phone?.OnUpdated(e: new Phone.UpdatedPhoneEventArgs(phone: phone));
		});
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetEmail, handler: (email) =>
		{
			if (SecurityIsLoaded)
				Security.Email?.OnUpdated(e: new Email.UpdatedEmailEventArgs(email: email));
		});
		_userHubConnection.On<int, int>(methodName: UserHubMethods.SendMessage, handler: async (chatId, messageId) =>
		{
			if (Chats.IsLoaded)
			{
				await Chats.OnReceivedMessage(
					e: new ChatCollection.ReceivedMessageInChatEventArgs(chatId: chatId, messageId: messageId),
					cancellationToken: cancellationToken
				);
			}
		});
	}
	#endregion
}