using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class LastMessage
{
	public string? Content { get; init; }
	public bool IsFile { get; init; }
	public DateTime CreatedAt { get; init; }
	public bool FromMe { get; init; }
	public bool IsRead { get; init; }
}

public sealed class Chat : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly Lazy<MessageCollection> _messages;
	#endregion

	#region Constructor
	private Chat(
		ApiClient client,
		IFileService fileService,
		ChatResponse response,
		Lazy<MessageCollection> messages
	)
	{
		_client = client;
		_fileService = fileService;

		Id = response.Id;
		ChatName = response.ChatName;
		ChatPhoto = response.ChatPhoto;
		LastMessage = response.LastMessage;
		CountOfUnreadMessages = response.CountOfUnreadMessages;
		_messages = messages;
	}
	#endregion

	#region Properties
	public int Id { get; init; }
	public string? ChatName { get; init; }
	public string? ChatPhoto { get; init; }
	public LastMessage? LastMessage { get; init; }
	public int CountOfUnreadMessages { get; init; }
	public MessageCollection Messages => _messages.Value;
	#endregion

	#region Records
	internal sealed record ChatResponse(int Id, string? ChatName, string? ChatPhoto, LastMessage? LastMessage, int CountOfUnreadMessages);
	#endregion

	#region Classes
	public sealed class ReceivedMessageEventArgs(int messageId) : EventArgs
	{
		public int MessageId { get; } = messageId;
	}
	#endregion

	#region Delegates
	public delegate void ReceivedMessageHandler(ReceivedMessageEventArgs e);
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
			fileService: fileService,
			response: response,
			messages: new Lazy<MessageCollection>(value: await MessageCollection.Create(
				client: client,
				chatId: id,
				cancellationToken: cancellationToken
			))
		);
	}

	public async Task Read(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PutAsync(
			apiMethod: ChatControllerMethods.ReadChat(chatId: Id),
			cancellationToken: cancellationToken
		);
	}

	private async Task Send(
		Message.MessageContent content,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PostAsync<MessageCollection.SendMessageRequest>(
			apiMethod: MessageControllerMethods.SendMessage,
			arg: new MessageCollection.SendMessageRequest(
				ChatId: Id,
				Content: content
			),
			cancellationToken: cancellationToken
		);
	}

	public async Task Send(
		ITextContent content,
		CancellationToken cancellationToken = default(CancellationToken)
	) => await Send(content: new Message.MessageContent(Text: content.Text, Attachments: null), cancellationToken: cancellationToken);

	public async Task Send(
		IFileContent content,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<Message.MessageAttachment> attachments = new List<Message.MessageAttachment>();
		foreach (Attachment contentFile in content.Attachments!)
			attachments.Add(await contentFile.Load(fileService: _fileService, cancellationToken: cancellationToken));

		await Send(content: new Message.MessageContent(
			Text: null,
			Attachments: attachments
		), cancellationToken: cancellationToken);
	}

	public async Task Send<T>(
		T content,
		CancellationToken cancellationToken = default(CancellationToken)
	) where T: IFileContent, ITextContent
	{
		List<Message.MessageAttachment> attachments = new List<Message.MessageAttachment>();
		foreach (Attachment contentFile in content.Attachments!)
			attachments.Add(await contentFile.Load(fileService: _fileService, cancellationToken: cancellationToken));

		await Send(new Message.MessageContent(
			Text: content.Text,
			Attachments: attachments
		), cancellationToken: cancellationToken);
	}

	internal void OnReceivedMessage(ReceivedMessageEventArgs e)
		=> ReceivedMessage?.Invoke(e: e);
	#endregion
}