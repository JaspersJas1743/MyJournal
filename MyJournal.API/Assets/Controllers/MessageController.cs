using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.Utilities;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;

namespace MyJournal.API.Assets.Controllers;

[Authorize]
[ApiController]
[Route(template: "api/message")]
public sealed class MessageController(
	MyJournalContext context,
	IHubContext<UserHub, IUserHub> userHubContext
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Records
	[Validator<GetMessagesRequestValidator>]
	public record GetMessagesRequest(int ChatId, int Offset, int Count);
	public record Sender(int Id, string Surname, string Name, string? Patronymic);
	public record MessageAttachment(string LinkToFile, AttachmentTypes AttachmentType);
	public record MessageContent(string? Text, IEnumerable<MessageAttachment>? Attachments);
	public record GetMessageResponse(MessageContent Content, DateTime CreatedAt, Sender Sender, bool FromMe, bool IsRead);

	[Validator<SendMessageRequestValidator>]
	public record SendMessageRequest(int ChatId, MessageContent Content);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// Получение списка последних сообщений из чата
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/message/get?ChatId=1&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	ChatId - идентификатор чата, сообщения которого необходимо получить
	///	Offset - смещение, начиная с которого будет происходить выборка потенциальных собеседников
	///	Count - максимальное количество возвращаемых потенциальных собеседников
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список последних сообщений в чате</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetMessageResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetMessageResponse>>> GetMessages(
		[FromQuery] GetMessagesRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<Message> messagesFromChat = _context.Messages
			.Where(predicate: m => m.ChatId.Equals(request.ChatId))
			.OrderByDescending(keySelector: m => m.CreatedAt);

		IQueryable<GetMessageResponse> messages = messagesFromChat
			.Skip(count: request.Offset).Take(count: request.Count)
			.Select(selector: m => new GetMessageResponse(
				new MessageContent(
					m.Text,
					m.Attachments.Select(a => new MessageAttachment(a.Link, a.AttachmentType.Type))
				),
				m.CreatedAt,
				new Sender(m.Sender.Id, m.Sender.Surname, m.Sender.Name, m.Sender.Patronymic),
				m.SenderId == userId,
				m.ReadedAt != null
			));

		return Ok(value: messages);
	}

	/// <summary>
	/// Получение списка последних сообщений из чата
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/message/{id:int}/get
	///
	/// Параметры:
	///
	///	id - идентификатор сообщения, более подробную информацию о котором необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Более подробная информация о сообщении</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "{id:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetMessageResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetMessageResponse>> GetMessageById(
		[FromRoute] int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		return Ok(value: await _context.Messages
			.AsNoTracking().AsSplitQuery()
			.Where(predicate: m => m.Id == id && m.SenderId == userId)
			.Select(selector: m => new GetMessageResponse(
				new MessageContent(
					m.Text,
					m.Attachments.Select(a => new MessageAttachment(a.Link, a.AttachmentType.Type))
				),
				m.CreatedAt,
				new Sender(m.Sender.Id, m.Sender.Surname, m.Sender.Name, m.Sender.Patronymic),
				m.SenderId == userId,
				m.ReadedAt != null
			)).SingleOrDefaultAsync(cancellationToken: cancellationToken)
		?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: $"Сообщение с идентификатором {id} не найдено."));
	}
	#endregion

	#region POST
	/// <summary>
	/// Отправить сообщение в чат
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/message/send
	///	{
	///		"ChatId": 1,
	///		"Content": {
	///			"Text": "Текст сообщения",
	///			"Attachments": [
	///				{
	///					"LinkToFile": "https://myjournal_assets.hb.ru-msk.vkcs.cloud/file.jpg",
	///					"AttachmentType": 0
	///				}
	///			]
	///		}
	///	}
	///
	/// Параметры:
	///
	///	ChatId - идентификатор чата, в который необходимо отправить сообщение
	///	Content - содержание сообщения
	///	Content.Text - текстовое содержимое сообщения
	///	Content.Attachments - файлы, прикрепленные к сообщению
	///	Content.Attachments.LinkToFile - ссылка на файл
	///	Content.Attachments.AttachmentType - тип файла:
	///		0 - Photo
	///		1 - Document
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Сообщение отправлено успешно</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPost(template: "send")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult> SendMessage(
		[FromBody] SendMessageRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<Attachment>? attachments = request.Content.Attachments?.Select(selector: a => new Attachment()
		{
			Link = a.LinkToFile,
			AttachmentType = _context.AttachmentTypes.Single(predicate: at => at.Type == a.AttachmentType)
		});

		Chat chat = await _context.Chats
			.Include(chat => chat.Users)
			.SingleAsync(
				predicate: c => c.Id == request.ChatId,
				cancellationToken: cancellationToken
			);

		Message sentMessage = new Message()
		{
			ChatId = chat.Id,
			Text = request.Content.Text,
			SenderId = GetAuthorizedUserId(),
			Attachments = (attachments ?? Enumerable.Empty<Attachment>()).ToList(),
			ReadedAt = chat.Users.Count == 1 ? DateTime.Now : null
		};

		await _context.Messages.AddAsync(entity: sentMessage, cancellationToken: cancellationToken);

		chat.LastMessageNavigation = sentMessage;
		chat.Attachments = sentMessage.Attachments;

		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.Users(
			userIds: chat.Users.Select(selector: u => u.Id.ToString())
		).SendMessage(chatId: chat.Id, messageId: sentMessage.Id);

		return Ok();
	}
	#endregion
	#endregion
}
