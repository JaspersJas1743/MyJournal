using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/task")]
public class TaskController(
	MyJournalContext context,
	IHubContext<TeacherHub, ITeacherHub> teacherHubContext,
	IHubContext<StudentHub, IStudentHub> studentHubContext,
	IHubContext<ParentHub, IParentHub> parentHubContext,
	IHubContext<AdministratorHub, IAdministratorHub> administratorHubContext
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Enums
	public enum AssignedTaskCompletionStatusRequest { All, Uncompleted, Completed, Expired }
	public enum AssignedTaskCompletionStatusResponse { Uncompleted, Completed, Expired }
	public enum CreatedTaskCompletionStatusRequest { All, Expired, NotExpired }
	#endregion

	#region Records
	public sealed record TaskAttachment(string LinkToFile, AttachmentTypes AttachmentType);
	public sealed record TaskContent(string? Text, IEnumerable<TaskAttachment>? Attachments);

	public sealed record GetAllAssignedTasksRequest(AssignedTaskCompletionStatusRequest CompletionStatus, int Offset, int Count);
	public sealed record GetAssignedTasksRequest(AssignedTaskCompletionStatusRequest CompletionStatus, int SubjectId, int Offset, int Count);
	public sealed record GetAssignedTasksResponse(int TaskId, string LessonName, DateTime ReleasedAt, TaskContent Content, AssignedTaskCompletionStatusResponse CompletionStatus);

	public sealed record GetAssignedToClassTasksRequest(CreatedTaskCompletionStatusRequest CompletionStatus, int SubjectId, int Offset, int Count);
	public sealed record GetAllAssignedToClassTasksRequest(CreatedTaskCompletionStatusRequest CompletionStatus, int Offset, int Count);

	public sealed record GetAllCreatedTasksRequest(CreatedTaskCompletionStatusRequest CompletionStatus, int Offset, int Count);
	public sealed record GetCreatedTasksRequest(CreatedTaskCompletionStatusRequest CompletionStatus, int SubjectId, int ClassId, int Offset, int Count);
	public sealed record GetCreatedTasksResponse(int TaskId, string ClassName, string LessonName, DateTime ReleasedAt, TaskContent Content, int CountOfCompletedTask, int CountOfUncompletedTask);

	public sealed record CreateTasksRequest(int SubjectId, int ClassId, TaskContent Content, DateTime ReleasedAt);
	public sealed record CreateTasksResponse(string Message);
	#endregion

	#region Methods
	#region AuxiliaryMethods
	private async Task<IQueryable<DatabaseModels.Task>> GetTasksForStudent(
		int userId,
		AssignedTaskCompletionStatusRequest completionStatusRequest,
		bool allSubject,
		int subjectId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<DatabaseModels.Task> tasks = _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Class.Tasks);

		if (!allSubject)
			tasks = tasks.Where(predicate: t => t.LessonId == subjectId);

		if (completionStatusRequest == AssignedTaskCompletionStatusRequest.Expired)
			return tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0);

		return tasks.Where(predicate: t => completionStatusRequest == AssignedTaskCompletionStatusRequest.All || t.TaskCompletionResults.Any(tcr =>
			tcr.Student.UserId == userId &&
			tcr.TaskCompletionStatus.CompletionStatus == Enum.Parse<TaskCompletionStatuses>(completionStatusRequest.ToString())
		));
	}

	private async Task<IQueryable<DatabaseModels.Task>> GetTasksForAdministrator(
		CreatedTaskCompletionStatusRequest completionStatusRequest,
		bool allSubject,
		int classId,
		int subjectId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<DatabaseModels.Task> tasks = _context.Classes.AsNoTracking()
			.Where(predicate: t => t.Id == classId)
			.SelectMany(selector: c => c.Tasks);

		if (!allSubject)
			tasks = tasks.Where(predicate: t => t.LessonId == subjectId);

		return completionStatusRequest switch
		{
			CreatedTaskCompletionStatusRequest.Expired => tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0),
			CreatedTaskCompletionStatusRequest.NotExpired => tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) >= 0),
			_ => tasks
		};
	}

	private async Task<IQueryable<DatabaseModels.Task>> GetTasksForParent(
		int userId,
		AssignedTaskCompletionStatusRequest completionStatusRequest,
		bool allSubject,
		int subjectId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<DatabaseModels.Task> tasks = _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.SelectMany(selector: p => p.Children.Class.Tasks);

		if (!allSubject)
			tasks = tasks.Where(predicate: t => t.LessonId == subjectId);

		if (completionStatusRequest == AssignedTaskCompletionStatusRequest.Expired)
			return tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0);

		return tasks.Where(predicate: t => completionStatusRequest == AssignedTaskCompletionStatusRequest.All || t.TaskCompletionResults.Any(tcr =>
			tcr.Student.Parents.Any(p => p.UserId == userId) &&
			tcr.TaskCompletionStatus.CompletionStatus == Enum.Parse<TaskCompletionStatuses>(completionStatusRequest.ToString())
		));
	}

	private async Task<IQueryable<DatabaseModels.Task>> GetTasksForTeacher(
		int userId,
		CreatedTaskCompletionStatusRequest completionStatusRequest,
		bool allSubject,
		int subjectId = 0,
		int classId = 0,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IQueryable<DatabaseModels.Task> tasks = _context.Teachers
			.Where(predicate: t => t.UserId == userId)
			.SelectMany(selector: t => t.Tasks);

		if (!allSubject)
			tasks = tasks.Where(predicate: t => t.ClassId == classId && t.LessonId == subjectId);

		return completionStatusRequest switch
		{
			CreatedTaskCompletionStatusRequest.Expired => tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0),
			CreatedTaskCompletionStatusRequest.NotExpired => tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) >= 0),
			_ => tasks
		};
	}
	#endregion

	#region GET
	/// <summary>
	/// Получение дополнительной информации о заданной задаче по ее идентификатору
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/get/{taskId:int}
	///
	/// Параметры:
	///
	///	taskId - идентификатор задачи, дополнительную информацию по которой необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Дополнительная информация о заданной задаче</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	/// <response code="404">Некорректный идентификатор задачи</response>
	[Authorize]
	[HttpGet(template: "assigned/get/{taskId:int}")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssignedTasksResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAssignedTasksResponse>> GetAssignedTaskById(
		[FromRoute] int taskId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DatabaseModels.Task task = await _context.Tasks.Where(predicate: t => t.Id == taskId).SingleOrDefaultAsync(cancellationToken: cancellationToken)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный идентификатор задачи.");

		return Ok(value: new GetAssignedTasksResponse(
			task.Id,
			task.Lesson.Name,
			task.ReleasedAt,
			new TaskContent(task.Text, task.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			(DateTime.Now - task.ReleasedAt).Days <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					task.TaskCompletionResults.Single(tcr => tcr.Student.UserId == userId).TaskCompletionStatus.CompletionStatus.ToString()
				)
		));
	}

	/// <summary>
	/// Получение дополнительной информации о заданной задаче по ее идентификатору
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/created/get/{taskId:int}
	///
	/// Параметры:
	///
	///	taskId - идентификатор задачи, дополнительную информацию по которой необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Дополнительная информация о заданной задаче</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	/// <response code="404">Некорректный идентификатор задачи</response>
	[Authorize]
	[HttpGet(template: "created/get/{taskId:int}")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetCreatedTasksResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetCreatedTasksResponse>> GetCreatedTaskById(
		[FromRoute] int taskId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DatabaseModels.Task task = await _context.Tasks.Where(predicate: t => t.Id == taskId).SingleOrDefaultAsync(cancellationToken: cancellationToken)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный идентификатор задачи.");

		return Ok(value: new GetCreatedTasksResponse(
			task.Id,
			task.Class.Name,
			task.Lesson.Name,
			task.ReleasedAt,
			new TaskContent(task.Text, task.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			task.TaskCompletionResults.Count(
				tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Completed
			),
			task.TaskCompletionResults.Count(
				tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Uncompleted
			)
		));
	}

	/// <summary>
	/// [Студент] Получение списка заданных задач с фильтрацией по дисциплине и статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/get?CompletionStatus=0&SubjectId=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Невыполненные задачи
	///		2 - Выполненные задачи
	///		3 - Завершенные задачи
	///	SubjectId - идентификатор дисциплины, список задач для которой необходимо получить
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список заданных задач с фильтрацией по дисциплине и статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "assigned/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetAssignedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetAssignedTasksResponse>>> GetAssignedTasks(
		[FromQuery] GetAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForStudent(
			userId: userId,
			subjectId: request.SubjectId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: false,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetAssignedTasksResponse(
			t.Id,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					t.TaskCompletionResults.Single(tcr => tcr.Student.UserId == userId).TaskCompletionStatus.CompletionStatus.ToString()
				)
		)));
	}

	/// <summary>
	/// [Студент] Получение списка заданных задач по всем дисциплинам с фильтрацией по статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/get/all?CompletionStatus=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Невыполненные задачи
	///		2 - Выполненные задачи
	///		3 - Завершенные задачи
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список заданных задач по всем дисциплинам с фильтрацией по статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "assigned/get/all")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetAssignedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetAssignedTasksResponse>>> GetAllAssignedTasks(
		[FromQuery] GetAllAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForStudent(
			userId: userId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: true,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetAssignedTasksResponse(
			t.Id,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					t.TaskCompletionResults.Single(tcr => tcr.Student.UserId == userId).TaskCompletionStatus.CompletionStatus.ToString()
				)
		)));
	}

	/// <summary>
	/// [Администратор] Получение списка заданных задач для указанного класса с фильтрацией по дисциплине и статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/class/{classId:int}/get?CompletionStatus=0&SubjectId=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список задач для которого необходимо получить
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Завершенные задачи
	///		2 - Незавершенные задачи
	///	SubjectId - идентификатор дисциплины, список задач для которой необходимо получить
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список заданных задач для указанного класса с фильтрацией по дисциплине и статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[HttpGet(template: "assigned/class/{classId:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetCreatedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetCreatedTasksResponse>>> GetAssignedByClassTasks(
		[FromRoute] int classId,
		[FromQuery] GetAssignedToClassTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForAdministrator(
			subjectId: request.SubjectId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: false,
			classId: classId,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetCreatedTasksResponse(
			t.Id,
			t.Class.Name,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Completed),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Uncompleted)
		)));
	}

	/// <summary>
	/// [Администратор] Получение списка заданных задач для указанного класса по всем дисциплинам с фильтрацией по статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/class/{classId:int}/get/all?CompletionStatus=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	classId - идентификатор класса, список задач для которого необходимо получить
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Завершенные задачи
	///		2 - Незавершенные задачи
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список заданных задач для указанного класса по всем дисциплинам с фильтрацией по статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Administrator</response>
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[HttpGet(template: "assigned/class/{classId:int}/get/all")]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetCreatedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetCreatedTasksResponse>>> GetAllAssignedByClassTasks(
		[FromRoute] int classId,
		[FromQuery] GetAllAssignedToClassTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForAdministrator(
			completionStatusRequest: request.CompletionStatus,
			allSubject: true,
			classId: classId,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetCreatedTasksResponse(
			t.Id,
			t.Class.Name,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Completed),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Uncompleted)
		)));
	}

	/// <summary>
	/// [Родитель] Получение списка заданных подопечному задач с фильтрацией по дисциплине и статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/children/get?CompletionStatus=0&SubjectId=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Невыполненные задачи
	///		2 - Выполненные задачи
	///		3 - Завершенные задачи
	///	SubjectId - идентификатор дисциплины, список задач для которой необходимо получить
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список заданных подопечному задач с фильтрацией по дисциплине и статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[HttpGet(template: "assigned/children/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetAssignedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetAssignedTasksResponse>>> GetAssignedTasksForChildren(
		[FromQuery] GetAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForParent(
			userId: userId,
			subjectId: request.SubjectId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: false,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetAssignedTasksResponse(
			t.Id,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					t.TaskCompletionResults.Single(tcr => tcr.Student.Parents.Any(p => p.UserId == userId)
				).TaskCompletionStatus.CompletionStatus.ToString())
		)));
	}

	/// <summary>
	/// [Родитель] Получение списка заданных подопечному задач по всем дисциплинам с фильтрацией по статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/assigned/children/get/all?CompletionStatus=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Невыполненные задачи
	///		2 - Выполненные задачи
	///		3 - Завершенные задачи
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список заданных подопечному задач по всем дисциплинам с фильтрацией по статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Parent</response>
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[HttpGet(template: "assigned/children/get/all")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetAssignedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetAssignedTasksResponse>>> GetAllAssignedTasksForChildren(
		[FromQuery] GetAllAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForParent(
			userId: userId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: true,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetAssignedTasksResponse(
			t.Id,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					t.TaskCompletionResults.Single(tcr => tcr.Student.Parents.Any(p => p.UserId == userId)
				).TaskCompletionStatus.CompletionStatus.ToString())
		)));
	}

	/// <summary>
	/// [Преподаватель] Получение списка созданных задач с фильтрацией по дисциплине и статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/created/get?CompletionStatus=0&SubjectId=0&ClassId=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Завершенные задачи
	///		2 - Незавершенные задачи
	///	SubjectId - идентификатор дисциплины, список задач для которой необходимо получить
	///	ClassId - идентификатор класса, список задач для которого необходимо получить
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список созданных задач с фильтрацией по дисциплине и статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "created/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetCreatedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetCreatedTasksResponse>>> GetCreatedTasks(
		[FromQuery] GetCreatedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForTeacher(
			userId: userId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: false,
			subjectId: request.SubjectId,
			classId: request.ClassId,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetCreatedTasksResponse(
			t.Id,
			t.Class.Name,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Completed),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Uncompleted)
		)));
	}

	/// <summary>
	/// [Преподаватель] Получение списка созданных задач по всем дисциплинам с фильтрацией по статусу выполнения
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/task/created/get/all?CompletionStatus=0&Offset=0&Count=20
	///
	/// Параметры:
	///
	///	CompletionStatus - статус выполнения задачи:
	///		0 - Все задачи
	///		1 - Завершенные задачи
	///		2 - Незавершенные задачи
	///	Offset - смещение, начиная с которого будет происходить выборка задач
	///	Count - максимальное количество возвращаемых задач
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список созданных задач по всем дисциплинам с фильтрацией по статусу выполнения</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "created/get/all")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<GetCreatedTasksResponse>))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetCreatedTasksResponse>>> GetAllCreatedTasks(
		[FromQuery] GetAllCreatedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForTeacher(
			userId: userId,
			completionStatusRequest: request.CompletionStatus,
			allSubject: true,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Skip(count: request.Offset).Take(count: request.Count).Select(selector: t => new GetCreatedTasksResponse(
			t.Id,
			t.Class.Name,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Completed),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Uncompleted)
		)));
	}
	#endregion

	#region POST
	/// <summary>
	/// [Преподаватель] Создание новой задачи для учеников
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	POST api/task/create
	/// {
	/// 	"SubjectId": 0,
	/// 	"ClassId": 0,
	/// 	"Content": {
	/// 		"Text": "string",
	/// 		"Attachments": [
	/// 			{
	/// 				"LinkToFile": "string",
	/// 				"AttachmentType": 0
	/// 			}
	/// 		]
	/// 	},
	/// 	"ReleasedAt": "2024-03-25T19:53:11.521Z"
	/// }
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, для которой создается задача
	///	ClassId - идентификатор класса, для которого создается задача
	///	Content - содержание новой задачи
	///	Content.Text - текстовое содержимое новой задачи
	///	Content.Attachments - файлы, прикрепленные к новой задаче
	///	Content.Attachments.LinkToFile - ссылка на файл
	///	Content.Attachments.AttachmentType - тип файла:
	///		0 - Document
	///		1 - Photo
	///	ReleasedAt - дата и время сдачи задания
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Сообщение об успешном создании новой задачи</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpPost(template: "create")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CreateTasksResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<CreateTasksResponse>> CreateTask(
		[FromBody] CreateTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<Attachment>? attachments = request.Content.Attachments?.Select(selector: a => new Attachment()
		{
			Link = a.LinkToFile,
			AttachmentType = _context.AttachmentTypes.Single(predicate: at => at.Type == a.AttachmentType)
		});

		int userId = GetAuthorizedUserId();
		int teacherId = await _context.Teachers.AsNoTracking()
			.Where(predicate: t => t.UserId == userId)
			.Select(selector: t => t.Id)
			.SingleAsync(cancellationToken: cancellationToken);
		DatabaseModels.Task newTask = new DatabaseModels.Task()
		{
			LessonId = request.SubjectId,
			ClassId = request.ClassId,
			CreatorId = teacherId,
			Attachments = (attachments ?? Enumerable.Empty<Attachment>()).ToList(),
			ReleasedAt = request.ReleasedAt,
			Text = request.Content.Text,
		};

		TaskCompletionStatus uncompleted = await _context.TaskCompletionStatuses.SingleAsync(
			predicate: tcs => tcs.CompletionStatus == TaskCompletionStatuses.Uncompleted,
			cancellationToken: cancellationToken
		);
		await _context.Classes.Where(predicate: c => c.Id == request.ClassId)
			.SelectMany(selector: c => c.Students)
			.ForEachAsync(action: s =>
			{
				TaskCompletionResult result = new TaskCompletionResult()
				{
					StudentId = s.Id,
					Task = newTask,
					TaskCompletionStatus = uncompleted
				};
				s.TaskCompletionResults.Add(item: result);
			}, cancellationToken: cancellationToken);

		await _context.Tasks.AddAsync(entity: newTask, cancellationToken: cancellationToken);
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IEnumerable<string> studentIds = _context.Students.AsNoTracking()
			.Where(predicate: s => s.ClassId == request.ClassId)
			.Select(selector: s => s.UserId.ToString());
		IQueryable<string> parentIds = _context.Users.Where(predicate: u => u.Id == userId)
			.SelectMany(selector: u => u.Parents.Select(p => p.UserId.ToString()));
		IQueryable<string> adminIds = _context.Administrators.Where(
			predicate: a => a.User.UserActivityStatus.ActivityStatus == UserActivityStatuses.Online
		).Select(selector: a => a.UserId.ToString());

		await studentHubContext.Clients.Users(userIds: studentIds).TeacherCreatedTask(taskId: newTask.Id, subjectId: newTask.LessonId);
		await teacherHubContext.Clients.User(userId: userId.ToString()).CreatedTask(taskId: newTask.Id, subjectId: newTask.LessonId);
		await parentHubContext.Clients.Users(userIds: parentIds).CreatedTaskToWard(taskId: newTask.Id, subjectId: newTask.LessonId);
		await administratorHubContext.Clients.Users(userIds: adminIds).CreatedTaskToStudents(taskId: newTask.Id, subjectId: newTask.LessonId, classId: newTask.ClassId);

		return Ok(value: new CreateTasksResponse(Message: "Задание успешно сохранено!"));
	}
	#endregion

	#region PUT
	/// <summary>
	/// [Студент] Изменение состояния выполнения задачи студентом
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/task/{taskId:int}/completion-status/change/{completionStatus}
	///
	/// Параметры:
	///
	///	taskId - идентификатор задачи, для которой изменяется состояние выполнения
	///	completionStatus - статус выполнения, который будет установлен для указанной задачи:
	///		Completed - задача выполнена
	///		Uncompleted - задача не выполнена
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Статус выполнения задачи успешно изменён</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	/// <response code="403">Некорректный идентификатор задачи</response>
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[HttpPut(template: "{taskId:int}/completion-status/change/{completionStatus}")]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult> ChangeCompletionStatusForTask(
		[FromRoute] int taskId,
		[FromRoute] string completionStatus,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		TaskCompletionResult completionResult = await _context.Tasks
			.Where(predicate: t => t.Id == taskId)
			.SelectMany(selector: t => t.TaskCompletionResults)
			.Where(predicate: tcr => tcr.Student.UserId == userId)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken)
			?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Некорректный идентификатор задачи.");

		TaskCompletionStatuses status = Enum.Parse<TaskCompletionStatuses>(value: completionStatus, ignoreCase: true);
		completionResult.TaskCompletionStatus = await _context.TaskCompletionStatuses.Where(
			predicate: tcr => tcr.CompletionStatus == status
		).SingleAsync(cancellationToken: cancellationToken);

		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IQueryable<string> parentIds = _context.Users.Where(predicate: u => u.Id == userId)
			.SelectMany(selector: u => u.Parents.Select(p => p.UserId.ToString()));
		IQueryable<string> adminIds = _context.Administrators.Where(
			predicate: a => a.User.UserActivityStatus.ActivityStatus == UserActivityStatuses.Online
		).Select(selector: a => a.UserId.ToString());
		string creatorId = await _context.Tasks.Where(predicate: t => t.Id == taskId)
			.Select(selector: t => t.Creator.UserId.ToString())
			.SingleAsync(cancellationToken: cancellationToken);
		if (status == TaskCompletionStatuses.Completed)
		{
			await studentHubContext.Clients.User(userId: userId.ToString()).CompletedTask(taskId: taskId);
			await teacherHubContext.Clients.User(userId: creatorId).StudentCompletedTask(taskId: taskId);
			await parentHubContext.Clients.Users(userIds: parentIds).WardCompletedTask(taskId: taskId);
			await administratorHubContext.Clients.Users(userIds: adminIds).StudentCompletedTask(taskId: taskId);
		}
		else
		{
			await studentHubContext.Clients.User(userId: userId.ToString()).UncompletedTask(taskId: taskId);
			await teacherHubContext.Clients.User(userId: creatorId).StudentUncompletedTask(taskId: taskId);
			await parentHubContext.Clients.Users(userIds: parentIds).WardUncompletedTask(taskId: taskId);
			await administratorHubContext.Clients.Users(userIds: adminIds).StudentUncompletedTask(taskId: taskId);
		}
		return Ok();
	}
	#endregion
	#endregion
}
