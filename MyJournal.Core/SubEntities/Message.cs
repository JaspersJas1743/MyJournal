using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed record Sender(int Id, string Surname, string Name, string Patronymic);

public sealed class Message : ISubEntity
{
	private Message(
		GetMessageResponse response,
		IEnumerable<Attachment>? attachments
	)
	{
		Id = response.MessageId;
		Text = response.Content.Text;
		Attachments = attachments;
		Sender = response.Sender;
		CreatedAt = response.CreatedAt;
		FromMe = response.FromMe;
		IsRead = response.IsRead;
	}

	public int Id { get; init; }
	public string? Text { get; set; }
	public IEnumerable<Attachment>? Attachments { get; init; }
	public Sender Sender { get; init; }
	public DateTime CreatedAt { get; init; }
	public bool FromMe { get; init; }
	public bool IsRead { get; init; }

	internal sealed record MessageAttachment(string? LinkToFile, SubEntities.Attachment.AttachmentType Type);
	internal sealed record MessageContent(string? Text, IEnumerable<MessageAttachment>? Attachments);
	internal sealed record GetMessageResponse(int MessageId, MessageContent Content, DateTime CreatedAt, Sender Sender, bool FromMe, bool IsRead);

	internal static async Task<Message> Create(
		ApiClient client,
		int messageId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetMessageResponse response = await client.GetAsync<GetMessageResponse>(
			apiMethod: MessageControllerMethods.GetMessageById(messageId: messageId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new Message(
			response: response,
			attachments: response.Content.Attachments?.Select(selector: a =>
				Attachment.Create(
					linkToFile: a.LinkToFile,
					type: a.Type
				).GetAwaiter().GetResult()
			));
	}
}