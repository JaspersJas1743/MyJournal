using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;

namespace MyJournal.API.Assets.Controllers;

[ApiController]
[Route(template: "api/timetable")]
public sealed class TimetableController(
	MyJournalContext context,
	ILogger<TimetableController> logger
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Records
	// [Validator<>]
	public sealed record GetTimetableByDateRequest(DateOnly Day);
	public sealed record GetTimetableBySubjectRequest(int SubjectId);

	public sealed record Subject(int Id, int Number, string ClassName, string Name, DateOnly Date, TimeSpan Start, TimeSpan End);
	public sealed record Estimation(string Grade);
	public sealed record Break(double CountMinutes);
	public sealed record GetTimetableForStudentAndParentResponse(Subject Subject, IEnumerable<Estimation> Assessments, Break? Break)
	{
		public Subject Subject { get; set; } = Subject;
		public IEnumerable<Estimation> Assessments { get; set; } = Assessments;
		public Break? Break { get; set; } = Break;
	}
	public sealed record GetTimetableForTeacherResponse(Subject Subject, Break? Break)
	{
		public Subject Subject { get; set; } = Subject;
		public Break? Break { get; set; } = Break;
	}
	#endregion

	#region Methods
	#region GET
	[HttpGet(template: "date/student/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<GetTimetableForStudentAndParentResponse>> GetTimetableByDateForStudent(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableForStudentAndParentResponse[] timings = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Class.LessonTimings)
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableForStudentAndParentResponse(
				new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				t.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					EF.Functions.DateDiffMillisecond(t.StartTime, a.Datetime.TimeOfDay) >= 0 &&
					EF.Functions.DateDiffMillisecond(t.EndTime, a.Datetime.TimeOfDay) <= 0 &&
					a.Student.UserId == userId
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableForStudentAndParentResponse> timetable = timings.Select(selector: (timing, index) =>
		{
			GetTimetableForStudentAndParentResponse? nextTiming = timings.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: timetable);
	}

	[HttpGet(template: "subject/student/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<GetTimetableForStudentAndParentResponse>> GetTimetableBySubjectForStudent(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableForStudentAndParentResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateTime.Now.AddDays(value: offset))
			.SelectMany(selector: d => _context.Students.AsNoTracking()
				.Where(predicate: s => s.UserId == userId)
				.SelectMany(selector: s => s.Class.LessonTimings)
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableForStudentAndParentResponse(
					new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, DateOnly.FromDateTime(d), t.StartTime, t.EndTime),
					t.Lesson.Assessments.Where(a =>
						a.Datetime.Date == d.Date &&
						EF.Functions.DateDiffMillisecond(t.StartTime, a.Datetime.TimeOfDay) >= 0 &&
						EF.Functions.DateDiffMillisecond(t.EndTime, a.Datetime.TimeOfDay) <= 0 &&
						a.Student.UserId == userId
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}

	[HttpGet(template: "date/parent/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	public async Task<ActionResult<GetTimetableForStudentAndParentResponse>> GetTimetableByDateForParent(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableForStudentAndParentResponse[] timings = await _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.SelectMany(selector: p => p.Children.Class.LessonTimings)
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableForStudentAndParentResponse(
				new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				t.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					EF.Functions.DateDiffMillisecond(t.StartTime, a.Datetime.TimeOfDay) >= 0 &&
					EF.Functions.DateDiffMillisecond(t.EndTime, a.Datetime.TimeOfDay) <= 0 &&
					a.Student.Parents.Any(p => p.UserId == userId)
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableForStudentAndParentResponse> timetable = timings.Select(selector: (t, i) =>
		{
			GetTimetableForStudentAndParentResponse? nextTiming = timings.ElementAtOrDefault(index: i + 1);
			if (nextTiming is not null)
				t.Break = new Break(CountMinutes: (nextTiming.Subject.Start - t.Subject.End).TotalMinutes);
			return t;
		});

		return Ok(value: timetable);
	}

	[HttpGet(template: "subject/parent/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	public async Task<ActionResult<GetTimetableForStudentAndParentResponse>> GetTimetableBySubjectForParent(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableForStudentAndParentResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateTime.Now.AddDays(value: offset))
			.SelectMany(selector: d => _context.Parents.AsNoTracking()
				.Where(predicate: p => p.UserId == userId)
				.SelectMany(selector: p => p.Children.Class.LessonTimings)
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableForStudentAndParentResponse(
					new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, DateOnly.FromDateTime(d), t.StartTime, t.EndTime),
					t.Lesson.Assessments.Where(a =>
						a.Datetime.Date == d.Date &&
						EF.Functions.DateDiffMillisecond(t.StartTime, a.Datetime.TimeOfDay) >= 0 &&
						EF.Functions.DateDiffMillisecond(t.EndTime, a.Datetime.TimeOfDay) <= 0 &&
						a.Student.Parents.Any(p => p.UserId == userId)
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}

	[HttpGet(template: "date/teacher/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult<GetTimetableForStudentAndParentResponse>> GetTimetableByDateForTeacher(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableForTeacherResponse[] timings = await _context.Teachers.AsNoTracking()
			.Where(predicate: t => t.UserId == userId)
			.SelectMany(selector: t => t.TeachersLessons)
			.SelectMany(selector: l => l.Lesson.LessonTimings.Intersect(l.Classes.SelectMany(c => c.LessonTimings)))
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableForTeacherResponse(
				new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableForTeacherResponse> timetable = timings.Select(selector: (timing, index) =>
		{
			GetTimetableForTeacherResponse? nextTiming = timings.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: timetable);
	}

	[HttpGet(template: "subject/teacher/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult<GetTimetableForStudentAndParentResponse>> GetTimetableBySubjectForTeacher(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableForTeacherResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateTime.Now.AddDays(value: offset))
			.SelectMany(selector: day => _context.Teachers.AsNoTracking()
				.Where(predicate: t => t.UserId == userId)
				.SelectMany(selector: t => t.TeachersLessons)
				.SelectMany(selector: l => l.Lesson.LessonTimings.Intersect(l.Classes.SelectMany(c => c.LessonTimings)))
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == day.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableForTeacherResponse(
					new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, DateOnly.FromDateTime(day), t.StartTime, t.EndTime),
					null
				))
			);
		return Ok(value: timetable);
	}
	#endregion
	#endregion
}