using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Collections;
using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.Constants.Hubs;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public class User
{
	#region Fields
	private readonly ApiClient _client;
	private readonly HubConnection _userHubConnection;

	private readonly AsyncLazy<PersonalData> _personalData;
	private readonly AsyncLazy<ChatCollection> _chats;
	private readonly AsyncLazy<InterlocutorCollection> _interlocutors;
	private readonly AsyncLazy<IntendedInterlocutorCollection> _intendedInterlocutors;
	private readonly AsyncLazy<Security> _security;
	private readonly AsyncLazy<ProfilePhoto> _photo;
	private readonly AsyncLazy<Activity> _activity;
	#endregion

	#region Constructors
	protected User(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		IFileService fileService,
		UserInformationResponse information
	)
	{
		client.ClientId = information.Id;
		fileService.ApiClient = client;
		_client = client;
		_userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(
			url: UserHubMethods.HubEndpoint,
			token: client.Token!
		);

		_personalData = new AsyncLazy<PersonalData>(valueFactory: async () => new PersonalData(
			surname: information.Surname,
			name: information.Name,
			patronymic: information.Patronymic
		));

		_chats = new AsyncLazy<ChatCollection>(valueFactory: async () => await ChatCollection.Create(
			client: client,
			fileService: fileService
		));
		_interlocutors = new AsyncLazy<InterlocutorCollection>(valueFactory: async () => await InterlocutorCollection.Create(
			client: client,
			fileService: fileService
		));
		_intendedInterlocutors = new AsyncLazy<IntendedInterlocutorCollection>(valueFactory: async () => await IntendedInterlocutorCollection.Create(
			client: client,
			fileService: fileService
		));
		_security = new AsyncLazy<Security>(valueFactory: async () => new Security(
			phone: new AsyncLazy<Phone>(valueFactory: async () => new Phone(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService,
				phone: information.Phone
			)),
			email: new AsyncLazy<Email>(valueFactory: async () => new Email(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService,
				email: information.Email
			)),
			password: new AsyncLazy<Password>(valueFactory: async () => new Password(
				client: client,
				googleAuthenticatorService: googleAuthenticatorService
			)),
			sessions: new AsyncLazy<SessionCollection>(valueFactory: async () => await SessionCollection.Create(
				   client: client
			))
		));
		_photo = new AsyncLazy<ProfilePhoto>(valueFactory: async () => new ProfilePhoto(
			client: client,
			fileService: fileService,
			link: information.Photo
		));
		_activity = new AsyncLazy<Activity>(valueFactory: async () => new Activity(
			client: client
		));
	}

	~User() => _client.Dispose();
	#endregion

	#region Records
	protected sealed record UserInformationResponse(int Id, string Surname, string Name, string? Patronymic, string? Phone, string? Email, string? Photo);
	#endregion

	#region Classes
	public sealed class JoinedInChatEventArgs(int chatId) : EventArgs
	{
		public int ChatId { get; } = chatId;
	}
	public sealed class ReceivedMessageEventArgs(int chatId, int messageId) : EventArgs
	{
		public int ChatId { get; } = chatId;
		public int MessageId { get; } = messageId;
	}
	#endregion

	#region Delegates
	public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
	public delegate void ReceivedMessageHandler(ReceivedMessageEventArgs e);
	#endregion

	#region Events
	public event JoinedInChatHandler? JoinedInChat;
	public event ReceivedMessageHandler? ReceivedMessage;
	#endregion

	#region Methods
	#region Get
	public async Task<PersonalData> GetPersonalData()
		=> await _personalData;

	public async Task<ChatCollection> GetChats()
		=> await _chats;

	public async Task<InterlocutorCollection> GetInterlocutors()
		=> await _interlocutors;

	public async Task<IntendedInterlocutorCollection> GetIntendedInterlocutors()
		=> await _intendedInterlocutors;

	public async Task<Security> GetSecurity()
		=> await _security;

	public async Task<ProfilePhoto> GetPhoto()
		=> await _photo;

	public async Task<Activity> GetActivity()
		=> await _activity;
	#endregion

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
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOnline, handler: async (userId, onlineAt) =>
		{
			await InvokeIfInterlocutorsAreCreated(invocation: async collection => await collection.OnAppearedOnline(
				e: new InterlocutorCollection.InterlocutorAppearedOnlineEventArgs(interlocutorId: userId, onlineAt: onlineAt)
			));
		});
		_userHubConnection.On<int, DateTime?>(methodName: UserHubMethods.SetOffline, handler: async (userId, onlineAt) =>
		{
			await InvokeIfInterlocutorsAreCreated(invocation: async collection => await collection.OnAppearedOffline(
				e: new InterlocutorCollection.InterlocutorAppearedOfflineEventArgs(interlocutorId: userId, onlineAt: onlineAt)
			));
		});
		_userHubConnection.On<int, string>(methodName: UserHubMethods.UpdatedProfilePhoto, handler: async (userId, link) =>
		{
			await InvokeIfInterlocutorsAreCreated(invocation: async collection => await collection.OnUpdatedPhoto(
				e: new InterlocutorCollection.InterlocutorUpdatedPhotoEventArgs(interlocutorId: userId, link: link)
			));
		});
		_userHubConnection.On<int>(methodName: UserHubMethods.DeletedProfilePhoto, handler: async userId =>
		{
			await InvokeIfInterlocutorsAreCreated(invocation: async collection => await collection.OnDeletedPhoto(
				e: new InterlocutorCollection.InterlocutorDeletedPhotoEventArgs(interlocutorId: userId)
			));
		});
		_userHubConnection.On<int>(methodName: UserHubMethods.SignIn, handler: async sessionId =>
		{
			await InvokeIfSessionsAreCreated(invocation: async collection => await collection.OnCreatedSession(
				e: new SessionCollection.CreatedSessionEventArgs(sessionId: sessionId),
				cancellationToken: cancellationToken
			));
		});
		_userHubConnection.On<IEnumerable<int>>(methodName: UserHubMethods.SignOut, handler: async sessionIds =>
		{
			await InvokeIfSessionsAreCreated(invocation: async collection => await collection.OnClosedSession(
				e: new SessionCollection.ClosedSessionEventArgs(sessionIds: sessionIds, currentSessionAreClosed: sessionIds.Contains(value: _client.SessionId)),
				cancellationToken: cancellationToken
			));
		});
		_userHubConnection.On<int>(methodName: UserHubMethods.JoinedInChat, handler: async chatId =>
		{
			await InvokeIfChatsAreCreated(invocation: async collection => await collection.Append(chatId: chatId, cancellationToken: cancellationToken));
			JoinedInChat?.Invoke(e: new JoinedInChatEventArgs(chatId: chatId));
		});
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetPhone, handler: async phone =>
			await InvokeIfPhoneIsCreated(invocation: async p => p.OnUpdated(e: new Phone.UpdatedPhoneEventArgs(phone: phone)))
		);
		_userHubConnection.On<string?>(methodName: UserHubMethods.SetEmail, handler: async email =>
			await InvokeIfEmailIsCreated(invocation: async e => e.OnUpdated(e: new Email.UpdatedEmailEventArgs(email: email)))
		);
		_userHubConnection.On<int, int>(methodName: UserHubMethods.SendMessage, handler: async (chatId, messageId) =>
		{
			await InvokeIfChatsAreCreated(invocation: async collection => await collection.OnReceivedMessage(
				e: new ChatCollection.ReceivedMessageInChatEventArgs(chatId: chatId, messageId: messageId),
				cancellationToken: cancellationToken
			));
			ReceivedMessage?.Invoke(e: new ReceivedMessageEventArgs(chatId: chatId, messageId: messageId));
		});
	}

	private async Task InvokeIfInterlocutorsAreCreated(
        Action<InterlocutorCollection> invocation
	)
	{
		if (!_interlocutors.IsValueCreated)
			return;

		InterlocutorCollection interlocutorCollection = await GetInterlocutors();
		invocation(obj: interlocutorCollection);
	}

	private async Task InvokeIfSessionsAreCreated(
        Func<SessionCollection, Task> invocation
	)
	{
		if (!_security.IsValueCreated)
			return;

		Security security = await GetSecurity();
		if (!security.SessionsAreCreated)
			return;

		SessionCollection sessionCollection = await security.GetSessions();
		await invocation(arg: sessionCollection);
	}

	private async Task InvokeIfPhoneIsCreated(
        Func<Phone, Task> invocation
	)
	{
		if (!_security.IsValueCreated)
			return;

		Security security = await GetSecurity();
		if (!security.PhoneIsCreated)
			return;

		Phone phone = await security.GetPhone();
		await invocation(arg: phone);
	}

	private async Task InvokeIfEmailIsCreated(
        Func<Email, Task> invocation
	)
	{
		if (!_security.IsValueCreated)
			return;

		Security security = await GetSecurity();
		if (!security.EmailIsCreated)
			return;

		Email email = await security.GetEmail();
		await invocation(arg: email);
	}

	private async Task InvokeIfChatsAreCreated(
        Func<ChatCollection, Task> invocation
	)
	{
		if (!_chats.IsValueCreated)
			return;

		ChatCollection chatCollection = await GetChats();
		await invocation(arg: chatCollection);
	}
	#endregion
}