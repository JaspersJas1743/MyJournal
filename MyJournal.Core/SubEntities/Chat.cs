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
	private readonly AsyncLazy<MessageCollection> _messages;
	#endregion

	#region Constructor
	private Chat(
		ApiClient client,
		ChatResponse response,
		AsyncLazy<MessageCollection> messages
	)
	{
		_client = client;
		Id = response.Id;
		Name = response.ChatName;
		Photo = response.ChatPhoto;
		LastMessage = response.LastMessage;
		IsSingleChat = response.AdditionalInformation.IsSingleChat;
		InterlocutorOnlineAt = response.AdditionalInformation.OnlineAt;
		CountOfParticipants = response.AdditionalInformation.CountOfParticipants;

		_messages = messages;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string? Name { get; init; }
	public string? Photo { get; init; }
	public LastMessage? LastMessage { get; init; }
	public bool IsSingleChat { get; init; }
	public DateTime? InterlocutorOnlineAt { get; init; }
	public int CountOfParticipants { get; init; }
	internal bool MessagesAreCreated => _messages.IsValueCreated;
	#endregion

	#region Records
	public record AdditionalInformation(bool IsSingleChat, DateTime? OnlineAt, int CountOfParticipants);
	public record ChatResponse(int Id, string ChatName, string ChatPhoto, LastMessage? LastMessage, AdditionalInformation AdditionalInformation);
	#endregion

	#region Events
	public event ReceivedMessageHandler? ReceivedMessage;
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
			response: response,
			messages: new AsyncLazy<MessageCollection>(valueFactory: async () => await MessageCollection.Create(
				client: client,
				fileService: fileService,
				chatId: id,
				cancellationToken: cancellationToken
			))
		);
	}

	public async Task<MessageCollection> GetMessages()
		=> await _messages;

	public async Task Read(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await _client.PutAsync(apiMethod: ChatControllerMethods.ReadChat(chatId: Id), cancellationToken: cancellationToken);

	internal void OnReceivedMessage(ReceivedMessageEventArgs e)
		=> ReceivedMessage?.Invoke(e: e);
	#endregion
}