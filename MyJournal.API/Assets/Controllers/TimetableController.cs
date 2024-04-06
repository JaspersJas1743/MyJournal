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

	public sealed record Subject(int Id, int Number, string Name, DateOnly Date, TimeSpan Start, TimeSpan End);
	public sealed record Estimation(string Grade);
	public sealed record Break(double CountMinutes);
	public sealed record GetTimetableResponse(Subject Subject, IEnumerable<Estimation> Assessments, Break? Break)
	{
		public Subject Subject { get; set; } = Subject;
		public IEnumerable<Estimation> Assessments { get; set; } = Assessments;
		public Break? Break { get; set; } = Break;
	}
	#endregion

	#region Methods
	#region GET
	[HttpGet(template: "date/me/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<GetTimetableResponse>> GetTimetableByDate(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableResponse[] timetable = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.SelectMany(selector: s => s.Class.LessonTimings)
			.Where(timing => (DayOfWeek)timing.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: timing => timing.Number)
			.Select(selector: timing => new GetTimetableResponse(
				new Subject(timing.LessonId, timing.Number, timing.Lesson.Name, request.Day, timing.StartTime, timing.EndTime),
				timing.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					EF.Functions.DateDiffMillisecond(timing.StartTime, a.Datetime.TimeOfDay) >= 0 &&
					EF.Functions.DateDiffMillisecond(timing.EndTime, a.Datetime.TimeOfDay) <= 0 &&
					a.Student.UserId == userId
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableResponse> result = timetable.Select(selector: (timing, index) =>
		{
			GetTimetableResponse? nextTiming = timetable.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: result);
	}

	[HttpGet(template: "subject/me/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<GetTimetableResponse>> GetTimetableBySubject(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		var timetable = Enumerable.Range(start: -3, count: 10).Select(selector: offset => DateTime.Now.AddDays(value: offset))
			.SelectMany(selector: day => _context.Students.AsNoTracking()
				.Where(predicate: s => s.UserId == userId)
				.SelectMany(selector: s => s.Class.LessonTimings)
				.Where(timing => (DayOfWeek)timing.DayOfWeekId == day.DayOfWeek && timing.LessonId == request.SubjectId)
				.OrderBy(keySelector: timing => timing.DayOfWeekId == 0 ? 7 : timing.DayOfWeekId)
				.ThenBy(keySelector: timing => timing.Number)
				.Select(selector: timing => new GetTimetableResponse(
					new Subject(timing.LessonId, timing.Number, timing.Lesson.Name, DateOnly.FromDateTime(day), timing.StartTime, timing.EndTime),
					timing.Lesson.Assessments.Where(a =>
						a.Datetime.Date == day.Date &&
						EF.Functions.DateDiffMillisecond(timing.StartTime, a.Datetime.TimeOfDay) >= 0 &&
						EF.Functions.DateDiffMillisecond(timing.EndTime, a.Datetime.TimeOfDay) <= 0 &&
						a.Student.UserId == userId
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}
	#endregion
	#endregion
}