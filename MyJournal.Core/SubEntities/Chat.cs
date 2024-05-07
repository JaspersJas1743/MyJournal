using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class LastMessage
{
	public string? Content { get; init; }
	public bool IsFile { get; init; }
	public DateTime CreatedAt { get; init; }
	public bool FromMe { get; init; }
	public bool IsRead { get; set; }
}

public sealed class Chat : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly AsyncLazy<MessageCollection> _messages;
	#endregion

	#region Constructor
	private Chat(
		ApiClient client,
		IFileService fileService,
		ChatResponse response,
		AsyncLazy<MessageCollection> messages
	)
	{
		_fileService = fileService;
		_client = client;
		Id = response.Id;
		Name = response.ChatName;
		Photo = response.ChatPhoto;
		CreatedAt = response.CreatedAt;
		LastMessage = response.LastMessage;
		IsSingleChat = response.AdditionalInformation.IsSingleChat;
		CurrentInterlocutorId = response.AdditionalInformation.InterlocutorId;
		CountOfParticipants = response.AdditionalInformation.CountOfParticipants;

		_messages = messages;
	}
	#endregion

	#region Properties
	public int? CurrentInterlocutorId { get; init; }
	public int Id { get; init; }
	public string? Name { get; init; }
	public string? Photo { get; init; }
	public DateTime? CreatedAt { get; init; }
	public LastMessage? LastMessage { get; set; }
	public bool IsSingleChat { get; init; }
	public Interlocutor? CurrentInterlocutor { get; private set; }
	public int CountOfParticipants { get; init; }
	internal bool MessagesAreCreated => _messages.IsValueCreated;
	#endregion

	#region Records
	public record AdditionalInformation(bool IsSingleChat, int? InterlocutorId, int CountOfParticipants);
	public record ChatResponse(int Id, string ChatName, string ChatPhoto, DateTime? CreatedAt, LastMessage? LastMessage, AdditionalInformation AdditionalInformation);
	#endregion

	#region Events
	public event ReceivedMessageHandler? ReceivedMessage;
	public event ReadChatHandler? ReadChat;
	#endregion

	#region Methods
	internal static async Task<Chat> Create(
		ApiClient client,
		IFileService fileService,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		ChatResponse response = await client.GetAsync<ChatResponse>(
			apiMethod: ChatControllerMethods.GetChat(chatId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new Chat(
			client: client,
			fileService: fileService,
			response: response,
			messages: new AsyncLazy<MessageCollection>(valueFactory: async () => await MessageCollection.Create(
				client: client,
				fileService: fileService,
				chatId: id,
				cancellationToken: cancellationToken
			))
		);
	}

	internal static Chat Create(
		ApiClient client,
		IFileService fileService,
		ChatResponse response,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return new Chat(
			client: client,
			fileService: fileService,
			response: response,
			messages: new AsyncLazy<MessageCollection>(valueFactory: async () => await MessageCollection.Create(
				client: client,
				fileService: fileService,
				chatId: response.Id,
				cancellationToken: cancellationToken
			))
		);
	}

	public async Task<MessageCollection> GetMessages()
		=> await _messages;

	public async Task Read(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await _client.PutAsync(apiMethod: ChatControllerMethods.ReadChat(chatId: Id), cancellationToken: cancellationToken);

	public async Task LoadInterlocutor(User user)
	{
		if (CurrentInterlocutorId is null)
			return;

		InterlocutorCollection userInterlocutors = await user.GetInterlocutors();
		CurrentInterlocutor = await userInterlocutors.FindById(id: CurrentInterlocutorId);
		if (CurrentInterlocutor is not null)
			return;

		CurrentInterlocutor = await Interlocutor.Create(client: _client, fileService: _fileService, id: CurrentInterlocutorId.Value);
		await userInterlocutors.Append(instance: CurrentInterlocutor);
	}

	internal async Task OnReceivedMessage(ReceivedMessageEventArgs e)
	{
		MessageCollection messages = await GetMessages();
		Message? message = await messages.FindById(id: e.MessageId);
		LastMessage = new LastMessage()
		{
			Content = message!.Text,
			CreatedAt = message.CreatedAt,
			FromMe = message.FromMe,
			IsFile = String.IsNullOrWhiteSpace(value: message.Text) && message.Attachments?.Any() == true,
			IsRead = message.IsRead
		};
		ReceivedMessage?.Invoke(e: e);
	}

	internal async Task OnReadChat(ReadChatEventArgs e)
	{
		await (await _messages).OnReadMessages();
		if (LastMessage is not null)
			LastMessage.IsRead = true;

		ReadChat?.Invoke(e: e);
	}
	#endregion
}