using System.Collections;
using System.Globalization;
using System.Net.Mime;
using System.Text;
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
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/assessments")]
public sealed class AssessmentController(
	MyJournalContext context,
	IHubContext<TeacherHub, ITeacherHub> teacherHubContext,
	IHubContext<StudentHub, IStudentHub> studentHubContext,
	IHubContext<ParentHub, IParentHub> parentHubContext,
	IHubContext<AdministratorHub, IAdministratorHub> administratorHubContext
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Records
	public sealed record Grade(int Id, string Assessment, DateTime CreatedAt, string? Comment, string Description, GradeTypes GradeType);

	public sealed record GetAssessmentsByIdResponse(string AverageAssessment, int? FinalAssessment, IEnumerable<Grade> Assessments);
	public sealed record Student(int StudentId, string Surname, string Name, string? Patronymic);
	public sealed record GetAssessmentsByClassResponse(Student Student, string AverageAssessment, int? FinalAssessment, IEnumerable<Grade> Assessments);

	[Validator<GetAverageAssessmentRequestValidator>]
	public sealed record GetAverageAssessmentRequest(int SubjectId, int PeriodId);
	public sealed record GetAverageAssessmentResponse(string AverageAssessment);

	[Validator<GetFinalAssessmentRequestValidator>]
	public sealed record GetFinalAssessmentRequest(int SubjectId, int PeriodId);
	public sealed record GetFinalAssessmentResponse(string? FinalAssessment);
	public sealed record GetFinalAssessmentByIdResponse(int? FinalAssessment);

	[Validator<GetAssessmentsRequestValidator>]
	public sealed record GetAssessmentsRequest(int PeriodId, int SubjectId);
	public sealed record GetAssessmentsResponse(string AverageAssessment, string? FinalAssessment, IEnumerable<Grade> Assessments);

	public sealed record GetAssessmentResponse(string AverageAssessment, Grade Assessment, int PeriodId);

	public sealed record GetPossibleAssessmentsResponse(int Id, string Assessment, GradeTypes GradeType, IEnumerable<GetCommentsForAssessmentsResponse> Comments);

	public sealed record GetCommentsForAssessmentsResponse(int Id, string? Comment, string Description);

	[Validator<CreateAssessmentRequestValidator>]
	public sealed record CreateAssessmentRequest(int GradeId, DateTime Datetime, int CommentId, int SubjectId, int StudentId);

	[Validator<CreateFinalAssessmentRequestValidator>]
	public sealed record CreateFinalAssessmentRequest(int GradeId, int SubjectId, int StudentId);

	[Validator<SetAttendanceRequestValidator>]
	public sealed record SetAttendanceRequest(int SubjectId, DateTime Datetime, IEnumerable<Attendance> Attendances);
	public sealed record Attendance(int StudentId, bool IsPresent, int? CommentId);

	[Validator<ChangeAssessmentRequestValidator>]
	public sealed record ChangeAssessmentRequest(int ChangedAssessmentId, int NewGradeId, DateTime Datetime, int CommentId);
	public sealed record ChangeAssessmentResponse(string Message);

	[Validator<DeleteAssessmentRequestValidator>]
	public sealed record DeleteAssessmentRequest(int AssessmentId);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// [Студент] Получение информации об оценках за указанный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/me/get?PeriodId=0&SubjectId=0
	///
	/// Параметры:
	///
	///	PeriodId - идентификатор учебного периода, за который необходимо получить информацию об оценках (0, если за текущий период)
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить информацию об оценках
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Информация об оценках</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "me/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAssessmentsResponse>> GetAssessments(
		[FromQuery] GetAssessmentsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		EducationPeriod period = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => ep.Id == request.PeriodId ||
				(request.PeriodId == 0 && EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)
			).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		DateTime start = period.StartDate.ToDateTime(time: TimeOnly.MinValue);
		DateTime end = period.EndDate.ToDateTime(time: TimeOnly.MaxValue);
		IQueryable<Assessment> assessments = _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Assessments)
			.Where(predicate: a =>
				a.LessonId == request.SubjectId && !a.IsDeleted &&
				EF.Functions.DateDiffDay(a.Datetime, start) <= 0 &&
				EF.Functions.DateDiffDay(a.Datetime, end) >= 0
			).OrderByDescending(keySelector: a => a.Datetime);

		double avg = await assessments.Where(predicate: a => EF.Functions.IsNumeric(a.Grade.Assessment))
			.Select(selector: a => a.Grade.Assessment).DefaultIfEmpty().Select(selector: g => g ?? "0")
			.AverageAsync(selector: a => Convert.ToDouble(a), cancellationToken: cancellationToken);

		string? final = await _context.FinalGradesForEducationPeriods
			.Where(predicate: fgfep =>
				fgfep.EducationPeriodId == period.Id &&
				fgfep.Student.UserId == userId &&
				fgfep.LessonId == request.SubjectId
			).Select(selector: fgfep => fgfep.Grade.Assessment)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		return Ok(value: new GetAssessmentsResponse(
			AverageAssessment: avg.ToString(format: "F2", CultureInfo.InvariantCulture).Replace(oldValue: "0.00", newValue: "-.--"),
			FinalAssessment: final is null ? "-.--" : final + ".00",
			Assessments: assessments.Select(selector: a => new Grade(
				a.Id,
				a.Grade.Assessment,
				a.Datetime,
				a.Comment.Comment,
				a.Comment.Description,
				a.Grade.GradeType.Type
			))
		));
	}

    /// <summary>
	/// [Ученик] Получение среднего балла по указанной дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/average/me/get?SubjectId=0
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить средний балл
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Средний балл по указанной дисциплине</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "average/me/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAverageAssessmentResponse>> GetAverageAssessment(
		[FromQuery] GetAverageAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		var period = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)			.Select(selector: ep => new
			{
				Start = ep.StartDate.ToDateTime(TimeOnly.MinValue),
				End = ep.EndDate.ToDateTime(TimeOnly.MaxValue)
			}).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		double avg = await _context.Students.AsNoTracking().Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Assessments).Where(predicate: a => !a.IsDeleted &&
				a.LessonId == request.SubjectId && EF.Functions.DateDiffDay(a.Datetime, period.Start) <= 0 &&
				EF.Functions.DateDiffDay(a.Datetime, period.End) >= 0 && EF.Functions.IsNumeric(a.Grade.Assessment)
			).Select(selector: a => a.Grade.Assessment).DefaultIfEmpty().Select(selector: g => g ?? "0")
			.AverageAsync(selector: a => Convert.ToDouble(a), cancellationToken: cancellationToken);

		return Ok(value: new GetAverageAssessmentResponse(
			AverageAssessment: avg.ToString(format: "F2", CultureInfo.InvariantCulture).Replace(oldValue: "0.00", newValue: "-.--")
		));
	}

    /// <summary>
	/// [Ученик] Получение итоговой отметки за указанный учебный период по указанной дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/final/me/get?SubjectId=0&PeriodId=0
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить средний балл
	///	PeriodId - идентификатор учебного периода, за который необходимо получить средний балл
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Итоговый балл по указанной дисциплине за указанный период</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "final/me/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetFinalAssessmentResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetFinalAssessmentResponse>> GetFinalAssessment(
		[FromQuery] GetFinalAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		string? final = await _context.FinalGradesForEducationPeriods
			.Where(predicate: fgfep =>
				fgfep.EducationPeriodId == request.PeriodId || ((request.PeriodId == 0
					&& EF.Functions.DateDiffDay(fgfep.EducationPeriod.StartDate, now) >= 0
					&& EF.Functions.DateDiffDay(fgfep.EducationPeriod.EndDate, now) <= 0
				)) &&
				fgfep.Student.UserId == userId &&
				fgfep.LessonId == request.SubjectId
			)
			.Select(selector: fgfep => fgfep.Grade.Assessment)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		return Ok(value: new GetFinalAssessmentResponse(FinalAssessment: final is null ? "-.--" : final + ".00"));
	}

	/// <summary>
	/// [Преподаватель/Администратор] Получение информации об оценках учеников по идентификатору класса за указанный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/student/{classId:int}/get?PeriodId=0&SubjectId=0
	///
	/// Параметры:
	///
	///	classId - идентификатор ученик, информацию об оценках которого необходимо получить
	///	PeriodId - идентификатор учебного периода, за который необходимо получить информацию об оценках (0, если за текущий период)
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить информацию об оценках
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Информация об оценках класса</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher или Administrator</response>
	/// <response code="404">Указанный учебный период не найден</response>
	[HttpGet(template: "class/{classId:int}/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsByIdResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult<IEnumerable<GetAssessmentsByClassResponse>>> GetAssessmentsByClass(
		[FromRoute] int classId,
		[FromQuery] GetAssessmentsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		var period = await _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == classId)
			.SelectMany(selector: c => c.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => ep.Id == request.PeriodId ||
				(request.PeriodId == 0 && EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)
			).Select(selector: ep => new
			{
				Id = ep.Id,
				Start = ep.StartDate.ToDateTime(TimeOnly.MinValue),
				End = ep.EndDate.ToDateTime(TimeOnly.MaxValue)
			}).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		IQueryable<GetAssessmentsByClassResponse> result = _context.Classes.AsNoTracking()
			.Where(predicate: c => c.Id == classId)
			.SelectMany(selector: s => s.Students)
			.OrderBy(keySelector: s => s.User.Surname)
			.ThenBy(keySelector: s => s.User.Surname)
			.ThenBy(keySelector: s => s.User.Patronymic)
			.Select(selector: s => new
			{
				Student = s,
				Average = s.Assessments.Where(a =>
						EF.Functions.IsNumeric(a.Grade.Assessment) &&
						a.LessonId == request.SubjectId && !a.IsDeleted &&
						EF.Functions.DateDiffDay(a.Datetime, period.Start) <= 0 &&
						EF.Functions.DateDiffDay(a.Datetime, period.End) >= 0
					).Select(a => a.Grade.Assessment).DefaultIfEmpty().Select(g => g ?? "0")
				    .Average(a => Convert.ToDouble(a)),
				Final = s.FinalGradesForEducationPeriods.Where(fgfep =>
					fgfep.EducationPeriodId == period.Id &&
					fgfep.Student.Id == s.Id &&
					fgfep.LessonId == request.SubjectId
				).Select(fgfep => fgfep.Grade.Id).SingleOrDefault(),
				Estimations = s.Assessments.Where(a =>
					a.LessonId == request.SubjectId && !a.IsDeleted &&
					EF.Functions.DateDiffDay(a.Datetime, period.Start) <= 0 &&
					EF.Functions.DateDiffDay(a.Datetime, period.End) >= 0
				).OrderByDescending(a => a.Datetime)
				.Select(a => new Grade(
					a.Id,
					a.Grade.Assessment,
					a.Datetime,
					a.Comment.Comment,
					a.Comment.Description,
					a.Grade.GradeType.Type
				))
			}).Select(selector: o => new GetAssessmentsByClassResponse(
				new Student(
					o.Student.Id,
					o.Student.User.Surname,
					o.Student.User.Name,
					o.Student.User.Patronymic
				),
				o.Average == 0 ? "-.--" : o.Average.ToString("F2", CultureInfo.InvariantCulture),
				o.Final,
				o.Estimations
			));

		return Ok(value: result);
	}

	/// <summary>
	/// [Преподаватель/Администратор] Получение информации об оценках по идентификатору ученика за указанный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/student/{studentId:int}/get?PeriodId=0&SubjectId=0
	///
	/// Параметры:
	///
	///	studentId - идентификатор ученик, информацию об оценках которого необходимо получить
	///	PeriodId - идентификатор учебного периода, за который необходимо получить информацию об оценках (0, если за текущий период)
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить информацию об оценках
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Информация об оценках ученика</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher или Administrator</response>
	/// <response code="404">Указанный учебный период не найден</response>
	[HttpGet(template: "student/{studentId:int}/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsByIdResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAssessmentsByIdResponse>> GetAssessmentsById(
		[FromRoute] int studentId,
		[FromQuery] GetAssessmentsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		var period = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.Id == studentId)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => ep.Id == request.PeriodId ||
				(request.PeriodId == 0 && EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)
			).Select(selector: ep => new
			{
				Id = ep.Id,
				Start = ep.StartDate.ToDateTime(TimeOnly.MinValue),
				End = ep.EndDate.ToDateTime(TimeOnly.MaxValue)
			}).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		IQueryable<Assessment> assessments = _context.Students.AsNoTracking()
			.Where(predicate: s => s.Id == studentId)
			.SelectMany(selector: s => s.Assessments)
			.Where(predicate: a =>
				a.LessonId == request.SubjectId && !a.IsDeleted &&
				EF.Functions.DateDiffDay(a.Datetime, period.Start) <= 0 &&
				EF.Functions.DateDiffDay(a.Datetime, period.End) >= 0
			).OrderByDescending(keySelector: a => a.Datetime);

		double avg = await assessments.Where(predicate: a => EF.Functions.IsNumeric(a.Grade.Assessment))
			.Select(selector: a => a.Grade.Assessment).DefaultIfEmpty().Select(selector: g => g ?? "0")
			.AverageAsync(selector: a => Convert.ToDouble(a), cancellationToken: cancellationToken);

		int? final = await _context.FinalGradesForEducationPeriods
			.Where(predicate: fgfep =>
				fgfep.EducationPeriodId == period.Id &&
				fgfep.Student.Id == studentId &&
				fgfep.LessonId == request.SubjectId
			).Select(selector: fgfep => fgfep.Grade.Id)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);


		return Ok(value: new GetAssessmentsByIdResponse(
			AverageAssessment: avg == 0 ? "-.--" : avg.ToString(format: "F2", CultureInfo.InvariantCulture),
			FinalAssessment: final,
			Assessments: assessments.Select(selector: a => new Grade(
				a.Id,
				a.Grade.Assessment,
				a.Datetime,
				a.Comment.Comment,
				a.Comment.Description,
				a.Grade.GradeType.Type
			))
		));
	}

	/// <summary>
	/// [Преподаватель/Администратор] Получение итогового балла за указанный период указанного ученика по указанной дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/final/student/{studentId:int}/get?SubjectId=0
	///
	/// Параметры:
	///
	///	studentId - идентификатор студента, средний балл которого необходимо получить
	///	SubjectId - идентификатор дисциплины, средний балл по которой необходимо получить
	///	PeriodId - идентификатор учебного периода, итоговый балл за который необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Итоговый балл указанного студента по указанной дисциплине</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "final/student/{studentId:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetFinalAssessmentByIdResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetFinalAssessmentByIdResponse>> GetFinalAssessmentById(
		[FromRoute] int studentId,
		[FromQuery] GetFinalAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		int? final = await _context.FinalGradesForEducationPeriods
			.Where(predicate: fgfep =>
				fgfep.EducationPeriodId == request.PeriodId || ((request.PeriodId == 0
					&& EF.Functions.DateDiffDay(fgfep.EducationPeriod.StartDate, now) >= 0
					&& EF.Functions.DateDiffDay(fgfep.EducationPeriod.EndDate, now) <= 0
				)) &&
				fgfep.Student.Id == studentId &&
				fgfep.LessonId == request.SubjectId
			).Select(selector: fgfep => fgfep.Grade.Id)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		return Ok(value: new GetFinalAssessmentByIdResponse(FinalAssessment: final));
	}

	/// <summary>
	/// [Преподаватель/Администратор] Получение среднего балла указанного ученика по указанной дисциплине
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/average/student/{studentId:int}/get?SubjectId=0
	///
	/// Параметры:
	///
	///	studentId - идентификатор студента, средний балл которого необходимо получить
	///	SubjectId - идентификатор дисциплины, средний балл по которой необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Средний балл указанного студента по указанной дисциплине</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "average/student/{studentId:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAverageAssessmentResponse>> GetAverageAssessmentById(
		[FromRoute] int studentId,
		[FromQuery] GetAverageAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		var period = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.Id == studentId)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)
			.Select(selector: ep => new
			{
				Start = ep.StartDate.ToDateTime(TimeOnly.MinValue),
				End = ep.EndDate.ToDateTime(TimeOnly.MaxValue)
			}).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		double avg = await _context.Students.AsNoTracking().Where(predicate: s => s.Id == studentId)
			.SelectMany(selector: s => s.Assessments).Where(predicate: a => !a.IsDeleted &&
				a.LessonId == request.SubjectId && EF.Functions.DateDiffDay(a.Datetime, period.Start) <= 0 &&
				EF.Functions.DateDiffDay(a.Datetime, period.End) >= 0 && EF.Functions.IsNumeric(a.Grade.Assessment)
			).Select(selector: a => a.Grade.Assessment).DefaultIfEmpty().Select(selector: g => g ?? "0")
			.AverageAsync(selector: a => Convert.ToDouble(a), cancellationToken: cancellationToken);

		return Ok(value: new GetAverageAssessmentResponse(
			AverageAssessment: avg.ToString(format: "F2", CultureInfo.InvariantCulture).Replace(oldValue: "0.00", newValue: "-.--")
		));
	}

	/// <summary>
	/// [Родитель] Получение информации об оценках подопечного за указанный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/ward/get?PeriodId=0&SubjectId=0
	///
	/// Параметры:
	///
	///	PeriodId - идентификатор учебного периода, за который необходимо получить информацию об оценках (0, если за текущий период)
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить информацию об оценках
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Информация об оценках подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "ward/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAssessmentsResponse>> GetAssessmentsForWard(
		[FromQuery] GetAssessmentsRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		EducationPeriod period = await _context.Parents.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Children.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => ep.Id == request.PeriodId ||
				(request.PeriodId == 0 && EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)
			).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		DateTime start = period.StartDate.ToDateTime(time: TimeOnly.MinValue);
		DateTime end = period.EndDate.ToDateTime(time: TimeOnly.MaxValue);
		IQueryable<Assessment> assessments = _context.Parents.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Children.Assessments)
			.Where(predicate: a =>
				a.LessonId == request.SubjectId && !a.IsDeleted &&
				EF.Functions.DateDiffDay(a.Datetime, start) <= 0 &&
				EF.Functions.DateDiffDay(a.Datetime, end) >= 0
			).OrderByDescending(keySelector: a => a.Datetime);

		double avg = await assessments.Where(predicate: a => EF.Functions.IsNumeric(a.Grade.Assessment))
			.Select(selector: a => a.Grade.Assessment).DefaultIfEmpty().Select(selector: g => g ?? "0")
			.AverageAsync(selector: a => Convert.ToDouble(a), cancellationToken: cancellationToken);

		string? final = await _context.FinalGradesForEducationPeriods
			.Where(predicate: fgfep =>
				fgfep.EducationPeriodId == period.Id &&
				fgfep.Student.Parents.Any(p => p.UserId == userId) &&
				fgfep.LessonId == request.SubjectId
			).Select(selector: fgfep => fgfep.Grade.Assessment)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		return Ok(value: new GetAssessmentsResponse(
			AverageAssessment: avg.ToString(format: "F2", CultureInfo.InvariantCulture).Replace(oldValue: "0.00", newValue: "-.--"),
			FinalAssessment: final is null ? "-.--" : final + ".00",
			Assessments: assessments.Select(selector: a => new Grade(
				a.Id,
				a.Grade.Assessment,
				a.Datetime,
				a.Comment.Comment,
				a.Comment.Description,
				a.Grade.GradeType.Type
			))
		));
	}

	/// <summary>
	/// [Родитель] Получение итогового балла подопечного за указанный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/final/ward/get?PeriodId=0&SubjectId=0
	///
	/// Параметры:
	///
	///	PeriodId - идентификатор учебного периода, за который необходимо получить информацию об оценках (0, если за текущий период)
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить информацию об оценках
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Итоговый балл подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "final/ward/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAverageAssessmentResponse>> GetFinalAssessmentForWard(
		[FromQuery] GetFinalAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		string? final = await _context.FinalGradesForEducationPeriods
			.Where(predicate: fgfep =>
				fgfep.EducationPeriodId == request.PeriodId || ((request.PeriodId == 0
					&& EF.Functions.DateDiffDay(fgfep.EducationPeriod.StartDate, now) >= 0
					&& EF.Functions.DateDiffDay(fgfep.EducationPeriod.EndDate, now) <= 0
				)) &&
				fgfep.Student.Parents.Any(p => p.UserId == userId) &&
				fgfep.LessonId == request.SubjectId
			).Select(selector: fgfep => fgfep.Grade.Assessment)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		return Ok(value: new GetFinalAssessmentResponse(FinalAssessment: final is null ? "-.--" : final + ".00"));
	}

	/// <summary>
	/// [Родитель] Получение среднего балла подопечного за указанный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/average/ward/get?PeriodId=0&SubjectId=0
	///
	/// Параметры:
	///
	///	PeriodId - идентификатор учебного периода, за который необходимо получить информацию об оценках (0, если за текущий период)
	///	SubjectId - идентификатор дисциплины, по которой необходимо получить информацию об оценках
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Средний балл подопечного</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Student</response>
	[HttpGet(template: "average/ward/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAverageAssessmentResponse>> GetAverageAssessmentsForWard(
		[FromQuery] GetAverageAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		var period = await _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.SelectMany(selector: p => p.Children.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => EF.Functions.DateDiffDay(ep.StartDate, now) >= 0 && EF.Functions.DateDiffDay(ep.EndDate, now) <= 0)
			.Select(selector: ep => new
			{
				Start = ep.StartDate.ToDateTime(TimeOnly.MinValue),
				End = ep.EndDate.ToDateTime(TimeOnly.MaxValue)
			}).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Указанный учебный период не найден."
			);

		double avg = await _context.Parents.AsNoTracking().Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Children.Assessments).Where(predicate: a => !a.IsDeleted &&
				a.LessonId == request.SubjectId && EF.Functions.DateDiffDay(a.Datetime, period.Start) <= 0 &&
				EF.Functions.DateDiffDay(a.Datetime, period.End) >= 0 && EF.Functions.IsNumeric(a.Grade.Assessment)
			).Select(selector: a => a.Grade.Assessment).DefaultIfEmpty().Select(selector: g => g ?? "0")
			.AverageAsync(selector: a => Convert.ToDouble(a), cancellationToken: cancellationToken);

		return Ok(value: new GetAverageAssessmentResponse(
			AverageAssessment: avg.ToString(format: "F2", CultureInfo.InvariantCulture).Replace(oldValue: "0.00", newValue: "-.--")
		));
	}

	/// <summary>
	/// [Преподаватель] Получение списка возможных оценок
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/possible/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список возможных оценок</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "possible/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetPossibleAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetPossibleAssessmentsResponse>> GetPossibleAssessments(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return Ok(value: _context.Grades.AsNoTracking().Select(
			selector: g => new GetPossibleAssessmentsResponse(
				g.Id,
				g.Assessment,
				g.GradeType.Type,
				g.GradeType.CommentsOnGrades.Select(c => new GetCommentsForAssessmentsResponse(
					c.Id,
					c.Comment,
					c.Description
				))
			)
		));
	}

	/// <summary>
	/// Получение списка возможных комментариев к оценке
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/{assessmentId:int}/comments/get
	///
	/// Параметры:
	///
	///	assessmentId - идентификатор оценки, список комментариев к которой необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список возможных комментариев к оценке</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[Authorize]
	[HttpGet(template: "{assessmentId:int}/comments/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetCommentsForAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetCommentsForAssessmentsResponse>> GetCommentsForAssessments(
		[FromRoute] int assessmentId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return Ok(value: _context.Grades.AsNoTracking().Where(predicate: g => g.Id == assessmentId)
			.SelectMany(selector: g => g.GradeType.CommentsOnGrades)
			.Select(selector: c => new GetCommentsForAssessmentsResponse(c.Id, c.Comment, c.Description))
		);
	}

	/// <summary>
	/// [Преподаватель] Получение списка комментариев к пропуску
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/truancy/comments/get
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Список возможных комментариев к пропуску</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpGet(template: "truancy/comments/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetCommentsForAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetCommentsForAssessmentsResponse>> GetCommentsForTruancy(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		return Ok(value: _context.GradeTypes.AsNoTracking().Where(predicate: gt => gt.Type == GradeTypes.Truancy)
			.SelectMany(selector: gt => gt.CommentsOnGrades)
			.Select(selector: c => new GetCommentsForAssessmentsResponse(c.Id, c.Comment, c.Description))
		);
	}

	/// <summary>
	/// Получение информации об оценке
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/assessments/{assessmentId:int}/get
	///
	/// Параметры:
	///
	///	assessmentId - идентификатор оценки, информацию о которой необходимо получить
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Информация об оценке</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="404">Оценка с указанным идентификатором не найдена</response>
	[Authorize]
	[HttpGet(template: "{assessmentId:int}/get")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GetAssessmentsResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult<GetAssessmentResponse>> Get(
		[FromRoute] int assessmentId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		var result = await _context.Assessments.AsNoTracking()
			.Where(predicate: a => a.Id == assessmentId && !a.IsDeleted)
			.Select(selector: a => new
			{
				Average = a.Student.Assessments.Where(asses => EF.Functions.IsNumeric(asses.Grade.Assessment) && asses.LessonId == a.LessonId)
					.Select(asses => asses.Grade.Assessment).DefaultIfEmpty().Select(assess => assess ?? "0")
					.Average(asses => Convert.ToDouble(asses)).ToString("F2", CultureInfo.InvariantCulture).Replace("0.00", "-.--"),
				Grade = new Grade(a.Id, a.Grade.Assessment, a.Datetime, a.Comment.Comment, a.Comment.Description, a.Grade.GradeType.Type),
				Receiver = a.StudentId
			}).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound, message: "Оценка с указанным идентификатором не найдена."
			);

		int period = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.Id == result.Receiver)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep =>
				EF.Functions.DateDiffDay(ep.StartDate, DateOnly.FromDateTime(result.Grade.CreatedAt)) >= 0 &&
				EF.Functions.DateDiffDay(ep.EndDate, DateOnly.FromDateTime(result.Grade.CreatedAt)) <= 0)
			.Select(selector: ep => ep.Id)
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

		return Ok(value: new GetAssessmentResponse(
			AverageAssessment: result.Average,
			Assessment: result.Grade,
			PeriodId: period
		));
	}
	#endregion

	#region POST
	/// <summary>
	/// [Преподаватель/Администратор] Установка новой оценки для ученика
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	POST api/assessments/create
	///	{
	///		"GradeId": 0,
	///		"Datetime": "2024-03-29T17:40:07.894Z",
	///		"CommentId": 0,
	///		"SubjectId": 0,
	///		"StudentId": 0,
	///	}
	///
	/// Параметры:
	///
	///	GradeId - идентификатор полученной оценки
	///	Datetime - дата, за которую необходимо установить оценку
	///	CommentId - идентификатор комментария к оценке
	///	SubjectId - идентификатор дисциплины, по которой получена оценка
	///	StudentId - идентификатор ученика, получившего оценку
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Оценка успешно установлена</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher или Administrator</response>
	[HttpPost(template: "create")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult> Create(
		[FromBody] CreateAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (
			await _context.Assessments.AsNoTracking().Where(predicate: a =>
				EF.Functions.DateDiffDay(a.Datetime, request.Datetime) == 0 &&
				!a.IsDeleted && a.Grade.GradeType.Type == GradeTypes.Truancy &&
				a.LessonId == request.SubjectId && a.GradeId == request.GradeId &&
				request.StudentId == a.StudentId
			).AnyAsync(cancellationToken: cancellationToken)
		)
		{
			string now = request.Datetime.ToString(format: "d MMMM", provider: CultureInfo.GetCultureInfo(name: "RU-ru"));
			throw new HttpResponseException(
				statusCode: StatusCodes.Status400BadRequest,
				message: $"Посещаемость за {now} для данного студента уже установлена."
			);
		}

		Assessment assessment = new Assessment()
		{
			GradeId = request.GradeId,
			CommentId = request.CommentId,
			Datetime = request.Datetime,
			LessonId = request.SubjectId,
			StudentId = request.StudentId
		};

		await _context.AddAsync(entity: assessment, cancellationToken: cancellationToken);
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IQueryable<string> studentId = _context.Students.Where(predicate: s => s.Id == request.StudentId)
			.Select(selector: s => s.UserId.ToString());

		IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == request.StudentId)
			.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

		IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

		await teacherHubContext.Clients.User(userId: GetAuthorizedUserId().ToString()).CreatedAssessment(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId
		);
		await studentHubContext.Clients.Users(userIds: studentId).TeacherCreatedAssessment(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId
		);
		await parentHubContext.Clients.Users(userIds: parentIds).CreatedAssessmentToWard(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId
		);
		await administratorHubContext.Clients.Users(userIds: adminIds).CreatedAssessmentToStudent(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId
		);
		return Ok();
	}

	/// <summary>
	/// [Преподаватель/Администратор] Установка итоговой оценки для ученика за текущий учебный период
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	POST api/assessments/final/create
	///	{
	///		"GradeId": 0,
	///		"SubjectId": 0,
	///		"StudentId": 0,
	///	}
	///
	/// Параметры:
	///
	///	GradeId - идентификатор полученной оценки
	///	SubjectId - идентификатор дисциплины, по которой получена оценка
	///	StudentId - идентификатор ученика, получившего оценку
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Итоговая оценка успешно установлена</response>
	/// <response code="400">Пользователь пытается установить оценку раньше возможного времени</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher или Administrator</response>
	[HttpPost(template: "final/create")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult> CreateFinalGrade(
		[FromBody] CreateFinalAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		DateOnly now = DateOnly.FromDateTime(dateTime: DateTime.Now);
		EducationPeriod period = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.Id == request.StudentId)
			.SelectMany(selector: s => s.Class.EducationPeriodForClasses)
			.Select(selector: epfc => epfc.EducationPeriod)
			.Where(predicate: ep => EF.Functions.DateDiffDay(ep.StartDate, now) >= 0
				&& EF.Functions.DateDiffDay(ep.EndDate, now) <= 0
				&& EF.Functions.DateDiffDay(now, ep.EndDate) <= 10
			).SingleOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status400BadRequest,
				message: "Устанавливать итоговую оценку можно только в пределах 10 дней до окончания учебного периода"
			);

		if (_context.FinalGradesForEducationPeriods.AsNoTracking().Any(predicate: fgfep =>
			fgfep.EducationPeriodId == period.Id &&
			fgfep.StudentId == request.StudentId &&
			fgfep.LessonId == request.SubjectId
		)) throw new HttpResponseException(
			statusCode: StatusCodes.Status400BadRequest,
			message: "Итоговая отметка за данный учебный период уже установлена."
		);

		FinalGradesForEducationPeriod assessment = new FinalGradesForEducationPeriod()
		{
			GradeId = request.GradeId,
			EducationPeriodId = period.Id,
			LessonId = request.SubjectId,
			StudentId = request.StudentId
		};

		await _context.AddAsync(entity: assessment, cancellationToken: cancellationToken);
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IQueryable<string> studentId = _context.Students.Where(predicate: s => s.Id == request.StudentId)
			.Select(selector: s => s.UserId.ToString());

		IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == request.StudentId)
			.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

		IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

		await teacherHubContext.Clients.User(userId: GetAuthorizedUserId().ToString()).CreatedFinalAssessment(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId,
			periodId: period.Id
		);
		await studentHubContext.Clients.Users(userIds: studentId).TeacherCreatedFinalAssessment(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId,
			periodId: period.Id
		);
		await parentHubContext.Clients.Users(userIds: parentIds).CreatedFinalAssessmentToWard(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId,
			periodId: period.Id
		);
		await administratorHubContext.Clients.Users(userIds: adminIds).CreatedFinalAssessmentToStudent(
            assessmentId: assessment.Id,
            studentId: request.StudentId,
            subjectId: assessment.LessonId,
			periodId: period.Id
		);
		return Ok();
	}
	#endregion

	#region PUT
	/// <summary>
	/// [Преподаватель/Администратор] Изменение оценки по ее идентификатору
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/assessments/change
	///	{
	///		"ChangedAssessmentId": 0,
	///		"NewAssessmentId": 0,
	///		"Datetime": "2024-03-29T17:40:07.894Z",
	///		"CommentId": 0
	///	}
	///
	/// Параметры:
	///
	///	ChangedAssessmentId - идентификатор заменяемой оценки
	///	NewAssessmentId - идентификатор оценки, на которую производится замена
	///	Datetime - дата, за которую установлена оценка
	///	CommentId - идентификатор комментария к оценке
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Оценка успешно изменена</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	/// <response code="404">Оценка с указанным идентификатором не найдена</response>
	[HttpPut(template: "change")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult<ChangeAssessmentResponse>> Change(
		[FromBody] ChangeAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Assessment assessment = await _context.Assessments
			.Include(assessment => assessment.Student)
			.SingleOrDefaultAsync(
				predicate: a => a.Id == request.ChangedAssessmentId && !a.IsDeleted,
				cancellationToken: cancellationToken
			) ?? throw new HttpResponseException(
				statusCode: StatusCodes.Status404NotFound,
				message: "Оценка с указанным идентификатором не найдена."
			);

		if (
			await _context.Assessments.AsNoTracking().Where(predicate: a =>
				EF.Functions.DateDiffDay(a.Datetime, request.Datetime) == 0 &&
				!a.IsDeleted && a.Grade.GradeType.Type == GradeTypes.Truancy &&
				a.LessonId == assessment.LessonId && a.GradeId == request.NewGradeId &&
				assessment.StudentId == a.StudentId
			).AnyAsync(cancellationToken: cancellationToken)
		)
		{
			string now = request.Datetime.ToString(format: "d MMMM", provider: CultureInfo.GetCultureInfo(name: "RU-ru"));
			throw new HttpResponseException(
				statusCode: StatusCodes.Status400BadRequest,
				message: $"Посещаемость за {now} для данного студента уже установлена."
			);
		}

		assessment.Datetime = request.Datetime == DateTime.MinValue ? assessment.Datetime : request.Datetime;
		assessment.CommentId = request.CommentId == -1 ? assessment.CommentId : request.CommentId;
		assessment.GradeId = request.NewGradeId == -1 ? assessment.GradeId : request.NewGradeId;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == assessment.StudentId)
			.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

		IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

		await teacherHubContext.Clients.User(userId: GetAuthorizedUserId().ToString()).ChangedAssessment(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);
		await studentHubContext.Clients.User(userId: assessment.Student.UserId.ToString()).TeacherChangedAssessment(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);
		await parentHubContext.Clients.Users(userIds: parentIds).ChangedAssessmentToWard(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);
		await administratorHubContext.Clients.Users(userIds: adminIds).ChangedAssessmentToStudent(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);

		return Ok(value: new ChangeAssessmentResponse(Message: "Оценка успешно изменена!"));
	}

	/// <summary>
	/// [Преподаватель] Установка посещаемости учеников
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/assessments/attendance/set
	///	{
	///		"SubjectId": 0,
	///		"Datetime": "2024-03-29T17:40:07.894Z",
	///		"Attendances": [
	///			{
	///				"StudentId": 0,
	///				"IsPresent": true,
	///				"CommentId": 0,
	///			}
	///		]
	///	}
	///
	/// Параметры:
	///
	///	SubjectId - идентификатор дисциплины, по которой устанавливается посещаемость
	///	Datetime - дата, за которую необходимо установить оценку
	///	Attendances - информация о посещаемости ученика
	///	Attendances.StudentId - идентификатор присутствовавшего/отсутствовавшего ученика
	///	Attendances.IsPresent - присутствие на занятии: true - присутствовал, false - отсутствовал
	///	Attendances.CommentId - идентификатор комментария к пропуску (если ученик отсутствовал)
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Посещаемость успешно проставлена</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher</response>
	[HttpPut(template: "attendance/set")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	public async Task<ActionResult> SetAttendance(
		[FromBody] SetAttendanceRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		DatabaseModels.Grade miss = await _context.Grades.AsNoTracking()
			.Where(predicate: g => g.GradeType.Type == GradeTypes.Truancy)
			.SingleAsync(cancellationToken: cancellationToken);

		DateTime dateTime = request.Datetime;
		IEnumerable<Attendance> attendances = request.Attendances;
		List<Assessment> list = await _context.Assessments
			.Where(predicate: a =>
				a.Grade.GradeType.Type == GradeTypes.Truancy && !a.IsDeleted &&
				EF.Functions.DateDiffDay(a.Datetime.Date, dateTime.Date) == 0
			).ToListAsync(cancellationToken: cancellationToken);
		var existing = list.Select(selector: a => new
		{
			New = attendances.FirstOrDefault(ra => ra.StudentId == a.StudentId),
			Old = a
		}).Where(predicate: o => o.New != null);

		List<Assessment> edition = new List<Assessment>();
		List<Assessment> deletion = new List<Assessment>();
		foreach (var o in existing)
		{
			o.Old.IsDeleted = o.New!.IsPresent;
			if (o.New!.IsPresent)
			{
				deletion.Add(item: o.Old);
				continue;
			}

			if (o.Old.CommentId == o.New!.CommentId)
				continue;
			o.Old.CommentId = o.New!.CommentId;
			edition.Add(item: o.Old);
		}

		List<Assessment> forAddition = attendances.Where(
			predicate: a => !a.IsPresent && existing.All(e => e.New!.StudentId != a.StudentId)
		).Select(selector: a => new Assessment()
		{
			GradeId = miss.Id,
			CommentId = a.CommentId,
			Datetime = request.Datetime,
			LessonId = request.SubjectId,
			StudentId = a.StudentId,
		}).ToList();
		await _context.AddRangeAsync(entities: forAddition, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		int userId = GetAuthorizedUserId();
		foreach (Assessment assessment in forAddition)
		{
			IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == assessment.StudentId)
				.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

			IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

			await teacherHubContext.Clients.User(userId: userId.ToString()).CreatedAssessment(
                assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await studentHubContext.Clients.User(userId: assessment.StudentId.ToString()).TeacherCreatedAssessment(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await parentHubContext.Clients.Users(userIds: parentIds).CreatedAssessmentToWard(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await administratorHubContext.Clients.Users(userIds: adminIds).CreatedAssessmentToStudent(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
		}

		foreach (Assessment assessment in deletion)
		{
			IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == assessment.StudentId)
				.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

			IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

			await teacherHubContext.Clients.User(userId: userId.ToString()).DeletedAssessment(
                assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await studentHubContext.Clients.User(userId: assessment.StudentId.ToString()).TeacherDeletedAssessment(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await parentHubContext.Clients.Users(userIds: parentIds).DeletedAssessmentToWard(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await administratorHubContext.Clients.Users(userIds: adminIds).DeletedAssessmentToStudent(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
		}

		foreach (Assessment assessment in edition)
		{
			IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == assessment.StudentId)
				.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

			IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

			await teacherHubContext.Clients.User(userId: userId.ToString()).ChangedAssessment(
                assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await studentHubContext.Clients.User(userId: assessment.StudentId.ToString()).TeacherChangedAssessment(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await parentHubContext.Clients.Users(userIds: parentIds).ChangedAssessmentToWard(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
			await administratorHubContext.Clients.Users(userIds: adminIds).ChangedAssessmentToStudent(
				assessmentId: assessment.Id,
				studentId: assessment.StudentId,
				subjectId: assessment.LessonId
			);
		}

		return Ok();
	}
	#endregion

	#region DELETE
	/// <summary>
	/// [Преподаватель/Администратор] Удаление оценки по ее идентификатору
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	POST api/assessments/delete
	///	{
	///		"AssessmentId": 0
	///	}
	///
	/// Параметры:
	///
	///	AssessmentId - идентификатор оценки, подлежащей удалению
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Оценка успешно удалена</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	/// <response code="403">Роль пользователя не соотвествует роли Teacher или Administrator</response>
	/// <response code="404">Оценка с указанным идентификатором не найдена</response>
	[HttpDelete(template: "delete")]
	[Authorize(Policy = nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator))]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ErrorResponse))]
	public async Task<ActionResult> Delete(
		[FromBody] DeleteAssessmentRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Assessment assessment = await _context.Assessments
			.Include(navigationPropertyPath: assessment => assessment.Student)
			.FirstOrDefaultAsync(
				predicate: a => a.Id == request.AssessmentId,
				cancellationToken: cancellationToken
			) ?? throw new HttpResponseException(statusCode: StatusCodes.Status404NotFound, message: "Оценка с указанным идентификатором не найдена.");

		assessment.IsDeleted = true;
		await _context.SaveChangesAsync(cancellationToken: cancellationToken);

		IQueryable<string> parentIds = _context.Students.Where(predicate: s => s.Id == assessment.StudentId)
			.SelectMany(selector: s => s.Parents).Select(selector: p => p.UserId.ToString());

		IQueryable<string> adminIds = _context.Administrators.Select(selector: a => a.UserId.ToString());

		await teacherHubContext.Clients.User(userId: GetAuthorizedUserId().ToString()).DeletedAssessment(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);
		await studentHubContext.Clients.User(userId: assessment.Student.UserId.ToString()).TeacherDeletedAssessment(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);
		await parentHubContext.Clients.Users(userIds: parentIds).DeletedAssessmentToWard(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);
		await administratorHubContext.Clients.Users(userIds: adminIds).DeletedAssessmentToStudent(
			assessmentId: assessment.Id,
			studentId: assessment.StudentId,
			subjectId: assessment.LessonId
		);

		return Ok();
	}
	#endregion
	#endregion
}