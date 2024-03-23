using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/task")]
public class TaskController(
	MyJournalContext context,
	IHubContext<TeacherHub, ITeacherHub> teacherHubContext,
	IHubContext<StudentHub, IStudentHub> studentHubContext
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

	public sealed record GetAllAssignedTasksRequest(AssignedTaskCompletionStatusRequest CompletionStatusRequest);
	public sealed record GetAllAssignedTasksResponse(int TaskId, string LessonName, DateTime ReleasedAt, TaskContent Content, AssignedTaskCompletionStatusResponse CompletionStatus);

	public sealed record GetAssignedTasksRequest(AssignedTaskCompletionStatusRequest CompletionStatusRequest, int SubjectId);
	public sealed record GetAssignedTasksResponse(int TaskId, DateTime ReleasedAt, TaskContent Content, AssignedTaskCompletionStatusResponse CompletionStatus);

	public sealed record GetAllCreatedTasksRequest(CreatedTaskCompletionStatusRequest CompletionStatusRequest);
	public sealed record GetAllCreatedTasksResponse(int TaskId, string ClassName, string LessonName, DateTime ReleasedAt, TaskContent Content, int CountOfCompletedTask, int CountOfUncompletedTask);

	public sealed record GetCreatedTasksRequest(CreatedTaskCompletionStatusRequest CompletionStatusRequest, int SubjectId, int ClassId);
	public sealed record GetCreatedTasksResponse(int TaskId, DateTime ReleasedAt, TaskContent Content, int CountOfCompletedTask, int CountOfUncompletedTask);

	public sealed record CreateTasksRequest(int SubjectId, int ClassId, TaskContent Content, DateTime ReleasedAt);
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

		if (completionStatusRequest == CreatedTaskCompletionStatusRequest.Expired)
			return tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0);

		if (completionStatusRequest == CreatedTaskCompletionStatusRequest.NotExpired)
			return tasks.Where(predicate: t => EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) >= 0);

		return tasks;
	}
	#endregion

	#region GET
	[HttpGet(template: "assigned/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<IEnumerable<GetAssignedTasksResponse>>> GetAssignedTasks(
		[FromQuery] GetAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForStudent(
			userId: userId,
			subjectId: request.SubjectId,
			completionStatusRequest: request.CompletionStatusRequest,
			allSubject: false,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Select(selector: t => new GetAssignedTasksResponse(
			t.Id,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					t.TaskCompletionResults.Single(tcr => tcr.Student.UserId == userId).TaskCompletionStatus.CompletionStatus.ToString()
				)
		)));
	}

	[HttpGet(template: "assigned/get/all")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<IEnumerable<GetAllAssignedTasksResponse>>> GetAllAssignedTasks(
		[FromQuery] GetAllAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForStudent(
			userId: userId,
			completionStatusRequest: request.CompletionStatusRequest,
			allSubject: true,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Select(selector: t => new GetAllAssignedTasksResponse(
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

	[HttpGet(template: "assigned/children/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	public async Task<ActionResult<IEnumerable<GetAssignedTasksResponse>>> GetAssignedTasksForChildren(
		[FromQuery] GetAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForParent(
			userId: userId,
			subjectId: request.SubjectId,
			completionStatusRequest: request.CompletionStatusRequest,
			allSubject: false,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Select(selector: t => new GetAssignedTasksResponse(
			t.Id,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			EF.Functions.DateDiffDay(DateTime.Now, t.ReleasedAt) <= 0
				? AssignedTaskCompletionStatusResponse.Expired
				: Enum.Parse<AssignedTaskCompletionStatusResponse>(
					t.TaskCompletionResults.Single(tcr => tcr.Student.Parents.Any(p => p.UserId == userId)
				).TaskCompletionStatus.CompletionStatus.ToString())
		)));
	}

	[HttpGet(template: "assigned/children/get/all")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	public async Task<ActionResult<IEnumerable<GetAllAssignedTasksResponse>>> GetAllAssignedTasksForChildren(
		[FromQuery] GetAllAssignedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForParent(
			userId: userId,
			completionStatusRequest: request.CompletionStatusRequest,
			allSubject: true,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Select(selector: t => new GetAllAssignedTasksResponse(
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

		[HttpGet(template: "created/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult<IEnumerable<GetCreatedTasksResponse>>> GetCreatedTasks(
		[FromQuery] GetCreatedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForTeacher(
			userId: userId,
			completionStatusRequest: request.CompletionStatusRequest,
			allSubject: false,
			subjectId: request.SubjectId,
			classId: request.ClassId,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Select(selector: t => new GetAllCreatedTasksResponse(
			t.Id,
			t.Class.Name,
			t.Lesson.Name,
			t.ReleasedAt,
			new TaskContent(t.Text, t.Attachments.Select(a => new TaskAttachment(a.Link, a.AttachmentType.Type))),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Completed),
			t.TaskCompletionResults.Count(tcr => tcr.TaskCompletionStatus.CompletionStatus == TaskCompletionStatuses.Uncompleted)
		)));
	}

	[HttpGet(template: "created/get/all")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult<IEnumerable<GetAllCreatedTasksResponse>>> GetAllCreatedTasks(
		[FromQuery] GetAllCreatedTasksRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IQueryable<DatabaseModels.Task> tasks = await GetTasksForTeacher(
			userId: userId,
			completionStatusRequest: request.CompletionStatusRequest,
			allSubject: true,
			cancellationToken: cancellationToken
		);

		return Ok(value: tasks.Select(selector: t => new GetAllCreatedTasksResponse(
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
	[HttpPost(template: "create")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult> CreateTask(
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

		DatabaseModels.TaskCompletionStatus uncompleted = await _context.TaskCompletionStatuses.SingleAsync(
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

		await teacherHubContext.Clients.Users(userIds: studentIds).CreatedTask(taskId: newTask.Id);

		return Ok();
	}
	#endregion

	#region PUT
	[Authorize(Policy = nameof(UserRoles.Student))]
	[HttpPut(template: "{taskId:int}/completion-status/change/{completionStatus}")]
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

		if (status == TaskCompletionStatuses.Completed)
			await studentHubContext.Clients.User(userId: userId.ToString()).CompletedTask(taskId: taskId);
		else
			await studentHubContext.Clients.User(userId: userId.ToString()).UncompletedTask(taskId: taskId);

		return Ok();
	}
	#endregion
	#endregion
}
