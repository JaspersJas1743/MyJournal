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
[Route(template: "api/chat")]
public sealed class ChatController(
	IHubContext<UserHub, IUserHub> userHubContext,
	MyJournalContext context
) : MyJournalBaseController(context: context)
{
	private const string GroupDefault = "https://myjournal_assets.hb.ru-msk.vkcs.cloud/defaults/group_default.png";
	private const string Favourites = "https://myjournal_assets.hb.ru-msk.vkcs.cloud/defaults/favourites.png";
	private const string UserDefault = "https://myjournal_assets.hb.ru-msk.vkcs.cloud/defaults/user_default.png";

	private readonly MyJournalContext _context = context;

	#region Records
	[Validator<GetDialogsRequestValidator>]
	public record GetChatsRequest(bool IsFiltered, string? Filter, int Offset, int Count);
	public record LastMessage(string? Content, bool IsFile, DateTime CreatedAt, bool FromMe, bool IsRead);
	public record AdditionalInformation(bool IsSingleChat, int? InterlocutorId, int CountOfParticipants);
	public record GetChatsResponse(int Id, string ChatName, string ChatPhoto, DateTime? CreatedAt, LastMessage? LastMessage, AdditionalInformation AdditionalInformation);

	[Validator<CreateSingleChatRequestValidator>]
	public record CreateSingleChatRequest(int InterlocutorId);

	[Validator<CreateMultiChatRequestValidator>]
	public record CreateMultiChatRequest(IEnumerable<int> InterlocutorIds, string? ChatName, string? LinkToPhoto);

	[Validator<GetIntendedInterlocutorsRequestValidator>]
	public record GetIntendedInterlocutorsRequest(bool IncludeExistedInterlocutors, bool IsFiltered, string? Filter, int Offset, int Count);
	public record GetIntendedInterlocutorsResponse(int Id, string Surname, string Name, string? Patronymic, string? Photo, UserActivityStatuses Activity, DateTime? OnlineAt);

	[Validator<GetInterlocutorsRequestValidator>]
	public record GetInterlocutorsRequest(int Offset, int Count);
	public record GetInterlocutorsResponse(int Id, string Surname, string Name, string? Patronymic, string? Photo, UserActivityStatuses Activity, DateTime? OnlineAt);
	#endregion

	#region Methods
	private async Task<ChatType> FindChatType(
		ChatTypes type,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return await _context.ChatTypes.SingleAsync(
			predicate: chatType => chatType.Type.Equals(type),
			cancellationToken: cancellationToken
		);
	}

	#region GET
	/// <summary>
	/// Получение списка последних диалогов пользователя
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///     GET api/chat/get?IsFiltered=true&Filter=%D0%B8&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	IsFiltered - логическое значение, отвечающее за наличие фильтрации данных по заданному критерию
	///	Filter - критерий, по которому будет проходить отбор последних диалогов
	///	Offset - смещение, начиная с которого будет происходить выборка диалогов
	///	Count - максимальное количество возвращаемых диалогов
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список последних диалогов пользователя</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetChatsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetChatsResponse>>> GetChats(
		[FromQuery] GetChatsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<User> users = _context.Users.AsNoTracking().Where(predicate: u => u.Id == userId);
		IQueryable<GetChatsResponse> chats = users.SelectMany(selector: u => u.Chats)
			.Select(selector: c => new { Chat = c, Interlocutor = c.Users.SingleOrDefault(u => u.Id != userId) })
			.Select(selector: c => new GetChatsResponse(
				c.Chat.Id,
				c.Chat.ChatType.Type == ChatTypes.Multi ? c.Chat.Name! : c.Chat.Users.Count == 1 ? "Избранное" : $"{c.Interlocutor!.Name} {c.Interlocutor.Surname}",
				c.Chat.ChatType.Type == ChatTypes.Multi ? c.Chat.LinkToPhoto ?? GroupDefault : c.Chat.Users.Count == 1 ? Favourites : c.Interlocutor!.LinkToPhoto ?? UserDefault,
				c.Chat.CreatedAt,
				c.Chat.LastMessageId.HasValue ? new LastMessage(
					c.Chat.LastMessageNavigation!.Text,
					String.IsNullOrWhiteSpace(c.Chat.LastMessageNavigation.Text) && c.Chat.LastMessageNavigation!.Attachments.Count != 0,
					c.Chat.LastMessageNavigation!.CreatedAt,
					c.Chat.LastMessageNavigation.Sender.Id == userId,
					c.Chat.LastMessageNavigation.ReadedAt != null
				) : null,
				new AdditionalInformation(
					c.Chat.ChatType.Type == ChatTypes.Single,
					c.Chat.ChatType.Type == ChatTypes.Single ? c.Interlocutor!.Id : null,
					c.Chat.Users.Count
				)
			));

		IEnumerable<GetChatsResponse> listOfChats = chats.AsEnumerable()
			.OrderByDescending(keySelector: r => r.LastMessage?.CreatedAt)
			.ThenByDescending(keySelector: r => r.CreatedAt);

		if (!request.IsFiltered)
			return Ok(value: listOfChats.Skip(count: request.Offset).Take(count: request.Count));

		User user = await users.SingleAsync(cancellationToken: cancellationToken);
		return Ok(value: listOfChats.Where(predicate: c =>
			c.ChatName.StartsWith(request.Filter!, StringComparison.CurrentCultureIgnoreCase) ||
			c.ChatName == "Избранное" && $"{user.Surname} {user.Name} {user.Patronymic}".Contains(
				request.Filter!,
				StringComparison.CurrentCultureIgnoreCase
			)
		).Skip(count: request.Offset).Take(count: request.Count));
	}

	/// <summary>
	/// Получение данных диалога по идентификатору
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///     GET api/chat/{id:int}/get
	///
	/// Параметры:
	///
	///	id - идентификатор диалога, информацию о котором необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Информация о диалоге</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "{id:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetChatsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetChatsResponse>> GetChat(
		[FromRoute] int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetChatsResponse? chat = await _context.Chats.AsNoTracking()
			.Where(predicate: c => c.Id == id)
			.Select(selector: c => new { Chat = c, Interlocutor = c.Users.SingleOrDefault(u => u.Id != userId) })
			.Select(selector: c => new GetChatsResponse(
				c.Chat.Id,
				c.Chat.ChatType.Type == ChatTypes.Multi ? c.Chat.Name! : c.Chat.Users.Count == 1 ? "Избранное" : $"{c.Interlocutor!.Name} {c.Interlocutor.Surname}",
				c.Chat.ChatType.Type == ChatTypes.Multi ? c.Chat.LinkToPhoto ?? GroupDefault : c.Chat.Users.Count == 1 ? Favourites : c.Interlocutor!.LinkToPhoto ?? UserDefault,
				c.Chat.CreatedAt,
				c.Chat.LastMessageId.HasValue ? new LastMessage(
					c.Chat.LastMessageNavigation!.Text,
					String.IsNullOrWhiteSpace(c.Chat.LastMessageNavigation.Text) && c.Chat.LastMessageNavigation!.Attachments.Count != 0,
					c.Chat.LastMessageNavigation!.CreatedAt,
					c.Chat.LastMessageNavigation.Sender.Id == userId,
					c.Chat.LastMessageNavigation.ReadedAt != null
				) : null,
				new AdditionalInformation(
					c.Chat.ChatType.Type == ChatTypes.Single,
					c.Chat.ChatType.Type == ChatTypes.Single ? c.Interlocutor!.Id : null,
					c.Chat.Users.Count
				)
			)).SingleOrDefaultAsync(cancellationToken: cancellationToken);
		return Ok(value: chat);
	}

	/// <summary>
	/// Получение списка собеседников пользователя
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/chat/interlocutors/get?Offset=0&Count=20
	///
	/// Параметры:
	///
	///	Offset - смещение, начиная с которого будет происходить выборка потенциальных собеседников
	///	Count - максимальное количество возвращаемых потенциальных собеседников
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список потенциальных собеседников пользователя</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "interlocutors/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetInterlocutorsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetInterlocutorsResponse>>> GetInterlocutors(
		[FromQuery] GetInterlocutorsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<User> interlocutors = _context.Users
			.Where(predicate: u => u.Id == userId)
			.SelectMany(selector: u => u.Chats.SelectMany(
				c => c.Users.Where(i => i.Id != userId || c.Users.Count == 1)
			)).Distinct().Skip(count: request.Offset).Take(count: request.Count);

		return Ok(interlocutors.Select(u => new GetInterlocutorsResponse(
			u.Id,
			u.Surname,
			u.Name,
			u.Patronymic,
			u.LinkToPhoto,
			u.UserActivityStatus.ActivityStatus,
			u.OnlineAt
		)));
	}

	/// <summary>
	/// Получение списка потенциальных собеседников пользователя
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/chat/interlocutors/get?IncludeExistedInterlocutors=true&IsFiltered=true&Filter=%D0%B8&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	IncludeExistedInterlocutors - логическое значение, отвечающее за наличие пользователей, с которыми уже имеется личный диалог, в конечной выборке
	///	IsFiltered - логическое значение, отвечающее за наличие фильтрации данных по заданному критерию
	///	Filter - критерий, по которому будет проходить отбор потенциальных собеседников
	///	Offset - смещение, начиная с которого будет происходить выборка потенциальных собеседников
	///	Count - максимальное количество возвращаемых потенциальных собеседников
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список потенциальных собеседников пользователя</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "intended-interlocutors/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetIntendedInterlocutorsResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetIntendedInterlocutorsResponse>>> GetIntendedInterlocutors(
		[FromQuery] GetIntendedInterlocutorsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		User user = await _context.Users
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.ChatType)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Users)
			.SingleOrDefaultAsync(predicate: u => u.Id.Equals(userId), cancellationToken: cancellationToken)
		?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		IQueryable<User> interlocutors = _context.Users.AsNoTracking()
			.Where(predicate: u => u.RegisteredAt != null && u.Id != userId);

		if (!request.IncludeExistedInterlocutors)
		{
			ChatType singleChatType = await FindChatType(type: ChatTypes.Single, cancellationToken: cancellationToken);
			IEnumerable<User> existedInterlocutors = user.Chats.Where(predicate: c => c.ChatType.Equals(singleChatType))
				.SelectMany(selector: c => c.Users.Count == 1 ? c.Users : c.Users.Except(second: new User[] { user }));
			interlocutors = interlocutors.Where(predicate: u => !existedInterlocutors.Contains(u));
		}

		if (request.IsFiltered)
		{
			interlocutors = interlocutors.Where(predicate: u =>
				EF.Functions.Like(u.Surname + ' ' + u.Name + ' ' + u.Patronymic, request.Filter + '%')
			);
		}

		return Ok(value: interlocutors.Skip(count: request.Offset).Take(count: request.Count)
			.Select(selector: u => new GetIntendedInterlocutorsResponse(
				u.Id,
				u.Surname,
				u.Name,
				u.Patronymic,
				u.LinkToPhoto,
				u.UserActivityStatus.ActivityStatus,
				u.OnlineAt
			)
		));
	}
	#endregion

	#region POST
	/// <summary>
	/// Создание нового личного диалога с пользователем
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	POST api/chat/single/create
	///	{
	///		"InterlocutorId": 0
	///	}
	///
	/// Параметры:
	///
	///	InterlocutorId - идентификатор пользователя, с которым необходимо создать личный диалог
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список последних диалогов пользователя с учётом новосозданного личного диалога</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="400">Диалог с указанным пользователем уже существует</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPost(template: "single/create")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetChatsResponse>>> CreateSingleChat(
		[FromBody] CreateSingleChatRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		User user = await _context.Users
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Users)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.ChatType)
			.SingleOrDefaultAsync(predicate: u => u.Id.Equals(userId), cancellationToken: cancellationToken)
		?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");

		ChatType singleChatType = await FindChatType(type: ChatTypes.Single, cancellationToken: cancellationToken);
		if (user.Chats.Any(predicate: c => c.Users.Any(predicate: u => u.Id == request.InterlocutorId && request.InterlocutorId != userId) && c.ChatType.Type == ChatTypes.Single))
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Данный диалог уже существует!");

		if (user.Chats.Any(predicate: c => c.Users.Count == 1) && request.InterlocutorId == userId)
			throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, message: "Данный диалог уже существует!");

		Chat createdChat = new Chat()
		{
			ChatType = singleChatType,
			Creator = user,
			Users = new List<User>() { user }
		};

		if (!user.Id.Equals(request.InterlocutorId))
		{
			User interlocutor = await FindUserByIdAsync(id: request.InterlocutorId, cancellationToken: cancellationToken)
				?? throw new HttpResponseException(statusCode: StatusCodes.Status400BadRequest, "Некорректный идентификатор собеседника.");
			createdChat.Users.Add(interlocutor);
		}
		await _context.Chats.AddAsync(entity: createdChat, cancellationToken: cancellationToken);
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.Users(userIds: createdChat.Users.Select(selector: c => c.Id.ToString()))
			.JoinedInChat(id: createdChat.Id);

		return Created();
	}

	/// <summary>
	/// Создание нового мультидиалога с пользователями
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	POST api/chat/multi/create
	///	{
	///		"InterlocutorIds": [
	///			1, 2, 3...
	///		],
	///		"ChatName": "your_new_chat_name",
	///		"LinkToPhoto": "https://myjournal_assets.hb.ru-msk.vkcs.cloud/ChatPhotos/filename.jpg"
	///	}
	///
	/// Параметры:
	///
	///	InterlocutorIds - список идентификаторов участников мультидиалога (идентификатор активного пользователя не обязателен)
	///	ChatName - наименование мультидиалога
	///	LinkToPhoto - ссылка на фотографию, которая будет отображаться в качестве аватара для мультидиалога (null при отсутствии)
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список последних диалогов пользователя с учётом новосозданного мультидиалога</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPost(template: "multi/create")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetChatsResponse>>> CreateMultiChat(
		[FromBody] CreateMultiChatRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		User user = await _context.Users
			.Include(user => user.Chats).ThenInclude(chat => chat.Users)
			.SingleOrDefaultAsync(predicate: u => u.Id.Equals(userId), cancellationToken: cancellationToken)
		?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");

		IQueryable<User> interlocutors = _context.Users.Where(predicate: u => request.InterlocutorIds.Contains(u.Id) || u.Id.Equals(userId));

		Chat createdChat = new Chat()
		{
			ChatType = await FindChatType(type: ChatTypes.Multi, cancellationToken: cancellationToken),
			Name = request.ChatName,
			Creator = user,
			Users = await interlocutors.ToListAsync(cancellationToken: cancellationToken),
			LinkToPhoto = request.LinkToPhoto
		};

		await _context.Chats.AddAsync(entity: createdChat, cancellationToken: cancellationToken);
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		await userHubContext.Clients.Users(userIds: interlocutors.Select(selector: c => c.Id.ToString()))
			.JoinedInChat(id: createdChat.Id);

		return Created();
	}
	#endregion

	#region PUT
	/// <summary>
	/// Пометить сообщения в чате как прочитанные
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/chat/{id:int}/read
	///
	/// Параметры:
	///
	///	Id - идентификатор чата, который необходимо пометить как прочитанный
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Чат отмечен как прочитанный</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "{id:int}/read")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult> ReadChat(
		[FromRoute] int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		await _context.Messages
			.Where(predicate: m => m.ChatId.Equals(id))
			.OrderByDescending(keySelector: m => m.CreatedAt)
			.Where(m => !m.SenderId.Equals(userId) && m.ReadedAt == null)
			.ForEachAsync(
				action: m => m.ReadedAt = DateTime.Now,
				cancellationToken: cancellationToken
			);

		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		int lastMessageSenderId = await _context.Chats.AsNoTracking()
			.Where(predicate: chat => chat.Id == id && chat.LastMessageId.HasValue)
			.Select(selector: chat => chat.LastMessageNavigation!.SenderId)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		if (lastMessageSenderId == 0)
			return Ok();

		await userHubContext.Clients.Users(user1: lastMessageSenderId.ToString()).ReadChat(chatId: id);

		return Ok();
	}
	#endregion
	#endregion
}
