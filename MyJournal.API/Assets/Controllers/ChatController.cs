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
	private readonly MyJournalContext _context = context;

	#region Records
	[Validator<GetDialogsRequestValidator>]
	public record GetChatsRequest(bool IsFiltered, string? Filter, int Offset, int Count);
	public record LastMessage(string? Content, bool IsFile, DateTime CreatedAt, bool FromMe, bool IsRead);
	public record GetChatsResponse(int Id, string ChatName, string ChatPhoto, LastMessage? LastMessage, int CountOfUnreadMessages);

	[Validator<CreateSingleChatRequestValidator>]
	public record CreateSingleChatRequest(int InterlocutorId);

	[Validator<CreateMultiChatRequestValidator>]
	public record CreateMultiChatRequest(IEnumerable<int> InterlocutorIds, string? ChatName, string? LinkToPhoto);

	[Validator<GetInterlocutorsRequestValidator>]
	public record GetInterlocutorsRequest(bool IncludeExistedInterlocutors, bool IsFiltered, string? Filter, int Offset, int Count);
	public record GetInterlocutorsResponse(int UserId, string Photo, string Name);
	#endregion

	#region Methods
	private string GetChatName(Chat chat, User currentUser)
	{
		if (chat.ChatType.Type == ChatTypes.Multi)
			return chat.Name!;

		if (chat.Users.Count == 1)
			return "Избранное";

		User interlocutor = chat.Users.Except(second: new User[1] { currentUser }).Single();
		return $"{interlocutor.Surname} {interlocutor.Name}";
	}

	private string GetChatPhoto(Chat chat, User currentUser)
	{
		if (chat.ChatType.Type == ChatTypes.Multi)
			return chat.LinkToPhoto ?? "https://myjournal_assets.hb.ru-msk.vkcs.cloud/Defaults/group_default.png";

		if (chat.Users.Count == 1)
			return "https://myjournal_assets.hb.ru-msk.vkcs.cloud/Defaults/favourites.png";

		User interlocutor = chat.Users.Except(second: new User[1] { currentUser }).Single();
		return interlocutor.LinkToPhoto ?? "https://myjournal_assets.hb.ru-msk.vkcs.cloud/Defaults/user_default.png";
	}

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
		User user = await _context.Users
			.AsSplitQuery()
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Users)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.ChatType)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.LastMessageNavigation)
				.ThenInclude(navigationPropertyPath: m => m.Sender)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Messages)
				.ThenInclude(navigationPropertyPath: m => m.Sender)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Messages)
				.ThenInclude(navigationPropertyPath: m => m.Attachments)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.LastMessageNavigation)
				.ThenInclude(navigationPropertyPath: m => m.Attachments)
			.SingleOrDefaultAsync(predicate: u => u.Id.Equals(userId), cancellationToken: cancellationToken)
		?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		IEnumerable<GetChatsResponse> result = user.Chats
			.OrderByDescending(keySelector: c => c.CreatedAt)
			.Skip(count: request.Offset).Take(count: request.Count).Select(chat => new GetChatsResponse(
				Id: chat.Id,
				ChatName: GetChatName(chat: chat, currentUser: user),
				ChatPhoto: GetChatPhoto(chat: chat, currentUser: user),
				LastMessage: chat.LastMessageId.HasValue ? new LastMessage(
					Content: chat.LastMessageNavigation?.Text,
					IsFile: chat.LastMessageNavigation?.Text is null && chat.LastMessageNavigation.Attachments.Any(),
					CreatedAt: chat.LastMessageNavigation.CreatedAt,
					FromMe: chat.LastMessageNavigation.Sender.Id.Equals(user.Id),
					IsRead: chat.LastMessageNavigation.ReadedAt is not null || chat.Users.Count == 1
				) : null,
				CountOfUnreadMessages: chat.Messages.OrderByDescending(keySelector: m => m.CreatedAt)
					.TakeWhile(predicate: m => m.ReadedAt is null && !m.Sender.Id.Equals(user.Id)).Count()
			));

		if (request.IsFiltered)
		{
			result = result.Where(predicate: r => r.ChatName.StartsWith(
				value: request.Filter, comparisonType: StringComparison.CurrentCultureIgnoreCase
			));
		}
		return Ok(value: result);
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
		User user = await GetAuthorizedUser(cancellationToken: cancellationToken);
		Chat chat = await _context.Chats
			.Include(navigationPropertyPath: c => c.ChatType)
			.Include(navigationPropertyPath: c => c.Users)
			.Where(predicate: c => c.Users.Contains(user))
			.SingleOrDefaultAsync(predicate: c => c.Id.Equals(id), cancellationToken: cancellationToken)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: $"Диалог с идентификатором {id} не найден.");
		return Ok(value: new GetChatsResponse(
			Id: chat.Id,
			ChatName: GetChatName(chat: chat, currentUser: user),
			ChatPhoto: GetChatPhoto(chat: chat, currentUser: user),
			LastMessage: null,
			CountOfUnreadMessages: 0
		));
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
		User user = await _context.Users
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.ChatType)
			.Include(navigationPropertyPath: u => u.Chats).ThenInclude(navigationPropertyPath: c => c.Users)
			.SingleOrDefaultAsync(predicate: u => u.Id.Equals(userId), cancellationToken: cancellationToken)
		?? throw new HttpResponseException(statusCode: StatusCodes.Status401Unauthorized, message: "Некорректный авторизационный токен.");
		IQueryable<User> interlocutors = _context.Users.AsNoTracking().AsSplitQuery();

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
				EF.Functions.Like(u.Surname + ' ' + u.Name + ' ' + u.Patronymic, request.Filter + '%') ||
				(u.Id.Equals(userId) && EF.Functions.Like("Избранное", request.Filter + '%'))
			);
		}

		return Ok(value: interlocutors.Skip(count: request.Offset).Take(count: request.Count).Select(selector: u => new GetInterlocutorsResponse(
			u.Id,
			u.Id.Equals(userId) ? "https://myjournal_assets.hb.ru-msk.vkcs.cloud/Defaults/favourites.png"
				: u.LinkToPhoto ?? "https://myjournal_assets.hb.ru-msk.vkcs.cloud/Defaults/user_default.png",
			u.Id.Equals(userId) ? "Избранное" : $"{u.Surname} {u.Name}"
		)));
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
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetChatsResponse>))]
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
		if (user.Chats.Any(predicate: c => c.Users.Any(predicate: u => u.Id.Equals(request.InterlocutorId)) && c.ChatType.Type == ChatTypes.Single))
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

		return RedirectToAction(
			actionName: nameof(GetChats),
			routeValues: new GetChatsRequest(IsFiltered: false, Filter: null, Offset: 0, Count: 20)
		);
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
	///	LinkToPhoto - ссылка на фотографию, которая будет отображаться в качестве аватара для мультидиалога
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список последних диалогов пользователя с учётом новосозданного мультидиалога</response>
	/// <response code="400">Некорректный авторизационный токен</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPost(template: "multi/create")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetChatsResponse>))]
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

		return RedirectToAction(
			actionName: nameof(GetChats),
			routeValues: new GetChatsRequest(IsFiltered: false, Filter: null, Offset: 0, Count: 20)
		);
	}
	#endregion
	#endregion
}
