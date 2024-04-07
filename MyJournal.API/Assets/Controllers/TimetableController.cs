using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Validation;

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
	public sealed record GetTimetableBySubjectAndClassRequest(int SubjectId, int ClassId);

	public sealed record Subject(int Id, int Number, string ClassName, string Name, DateOnly Date, TimeSpan Start, TimeSpan End);
	public sealed record Estimation(string Grade);
	public sealed record Break(double CountMinutes);
	public sealed record GetTimetableWithAssessmentsResponse(Subject Subject, IEnumerable<Estimation> Assessments, Break? Break)
	{
		public Subject Subject { get; set; } = Subject;
		public IEnumerable<Estimation> Assessments { get; set; } = Assessments;
		public Break? Break { get; set; } = Break;
	}
	public sealed record GetTimetableWithoutAssessmentsResponse(Subject Subject, Break? Break)
	{
		public Subject Subject { get; set; } = Subject;
		public Break? Break { get; set; } = Break;
	}

	// [Validator<>]
	public sealed record GetTimetableByClassRequest(int ClassId);
	public sealed record DayOfWeekOnTimetable(int Id, string Name);
	public sealed record SubjectInClass(int Number, string Name, TimeSpan Start, TimeSpan End);
	public sealed record TimetableInClass(SubjectInClass Subject, Break? Break)
	{
		public SubjectInClass Subject { get; set; } = Subject;
		public Break? Break { get; set; } = Break;
	}
	public sealed record GetTimetableByClassResponse(DayOfWeekOnTimetable DayOfWeek, int TotalHours, IEnumerable<TimetableInClass> Timetable)
	{
		public DayOfWeekOnTimetable DayOfWeek { get; set; } = DayOfWeek;
		public int TotalHours { get; set; } = TotalHours;
		public IEnumerable<TimetableInClass> Timetable { get; set; } = Timetable;
	}
	#endregion

	#region Methods
	#region GET
	[HttpGet(template: "date/student/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableByDateForStudent(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableWithAssessmentsResponse[] timings = await _context.Students.AsNoTracking()
			.Where(predicate: s => s.UserId == userId)
			.Where(predicate: s => !s.Class.EducationPeriodForClasses.Any(
				epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < request.Day && h.EndDate > request.Day)
			))
			.SelectMany(selector: s => s.Class.LessonTimings)
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableWithAssessmentsResponse(
				new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				t.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					t.StartTime <= a.Datetime.TimeOfDay &&
					t.EndTime >= a.Datetime.TimeOfDay &&
					a.Student.UserId == userId
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = timings.Select(selector: (timing, index) =>
		{
			GetTimetableWithAssessmentsResponse? nextTiming = timings.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: timetable);
	}

	[HttpGet(template: "subject/student/get")]
	[Authorize(Policy = nameof(UserRoles.Student))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableBySubjectForStudent(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset)))
			.SelectMany(selector: d => _context.Students.AsNoTracking()
				.Where(predicate: s => s.UserId == userId)
				.Where(predicate: s => !s.Class.EducationPeriodForClasses.Any(
					epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < d && h.EndDate > d)
				))
				.SelectMany(selector: s => s.Class.LessonTimings)
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableWithAssessmentsResponse(
					new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, d, t.StartTime, t.EndTime),
					t.Lesson.Assessments.Where(a =>
						DateOnly.FromDateTime(a.Datetime) == d &&
						t.StartTime <= a.Datetime.TimeOfDay &&
						t.EndTime >= a.Datetime.TimeOfDay &&
						a.Student.UserId == userId
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}

	[HttpGet(template: "date/parent/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableByDateForParent(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableWithAssessmentsResponse[] timings = await _context.Parents.AsNoTracking()
			.Where(predicate: p => p.UserId == userId)
			.Where(predicate: p => !p.Children.Class.EducationPeriodForClasses.Any(
				epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < request.Day && h.EndDate > request.Day)
			))
			.SelectMany(selector: p => p.Children.Class.LessonTimings)
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableWithAssessmentsResponse(
				new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				t.Lesson.Assessments.Where(a =>
					DateOnly.FromDateTime(a.Datetime) == request.Day &&
					t.StartTime <= a.Datetime.TimeOfDay &&
					t.EndTime >= a.Datetime.TimeOfDay &&
					a.Student.Parents.Any(p => p.UserId == userId)
				).Select(a => new Estimation(a.Grade.Assessment)),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = timings.Select(selector: (t, i) =>
		{
			GetTimetableWithAssessmentsResponse? nextTiming = timings.ElementAtOrDefault(index: i + 1);
			if (nextTiming is not null)
				t.Break = new Break(CountMinutes: (nextTiming.Subject.Start - t.Subject.End).TotalMinutes);
			return t;
		});

		return Ok(value: timetable);
	}

	[HttpGet(template: "subject/parent/get")]
	[Authorize(Policy = nameof(UserRoles.Parent))]
	public async Task<ActionResult<GetTimetableWithAssessmentsResponse>> GetTimetableBySubjectForParent(
		[FromQuery] GetTimetableBySubjectRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableWithAssessmentsResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset)))
			.SelectMany(selector: d => _context.Parents.AsNoTracking()
				.Where(predicate: p => p.UserId == userId)
				.Where(predicate: p => !p.Children.Class.EducationPeriodForClasses.Any(
					epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < d && h.EndDate > d)
				))
				.SelectMany(selector: p => p.Children.Class.LessonTimings)
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek && t.LessonId == request.SubjectId)
				.OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableWithAssessmentsResponse(
					new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, d, t.StartTime, t.EndTime),
					t.Lesson.Assessments.Where(a =>
						DateOnly.FromDateTime(a.Datetime) == d &&
						t.StartTime <= a.Datetime.TimeOfDay &&
						t.EndTime >= a.Datetime.TimeOfDay &&
						a.Student.Parents.Any(p => p.UserId == userId)
					).Select(a => new Estimation(a.Grade.Assessment)),
					null
				))
			);
		return Ok(value: timetable);
	}

	[HttpGet(template: "date/teacher/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult<GetTimetableWithoutAssessmentsResponse>> GetTimetableByDateForTeacher(
		[FromQuery] GetTimetableByDateRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		GetTimetableWithoutAssessmentsResponse[] timings = await _context.Teachers.AsNoTracking()
			.Where(predicate: t => t.UserId == userId)
			.SelectMany(selector: t => t.TeachersLessons)
			.SelectMany(selector: l => l.Lesson.LessonTimings.Intersect(l.Classes.SelectMany(c => c.LessonTimings)))
			.Where(predicate: t => !t.Class.EducationPeriodForClasses.Any(
				epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < request.Day && h.EndDate > request.Day)
			))
			.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == request.Day.DayOfWeek)
			.OrderBy(keySelector: t => t.Number)
			.Select(selector: t => new GetTimetableWithoutAssessmentsResponse(
				new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, request.Day, t.StartTime, t.EndTime),
				null
			)).ToArrayAsync(cancellationToken: cancellationToken);

		IEnumerable<GetTimetableWithoutAssessmentsResponse> timetable = timings.Select(selector: (timing, index) =>
		{
			GetTimetableWithoutAssessmentsResponse? nextTiming = timings.ElementAtOrDefault(index: index + 1);
			if (nextTiming is not null)
				timing.Break = new Break(CountMinutes: (nextTiming.Subject.Start - timing.Subject.End).TotalMinutes);
			return timing;
		});

		return Ok(value: timetable);
	}

	[HttpGet(template: "subject/teacher/get")]
	[Authorize(Policy = nameof(UserRoles.Teacher))]
	public async Task<ActionResult<GetTimetableWithoutAssessmentsResponse>> GetTimetableBySubjectForTeacher(
		[FromQuery] GetTimetableBySubjectAndClassRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		int userId = GetAuthorizedUserId();
		IEnumerable<GetTimetableWithoutAssessmentsResponse> timetable = Enumerable.Range(start: -3, count: 10)
			.Select(selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset)))
			.SelectMany(selector: d => _context.Teachers.AsNoTracking()
				.Where(predicate: t => t.UserId == userId)
				.SelectMany(selector: t => t.TeachersLessons)
				.SelectMany(selector: l => l.Lesson.LessonTimings.Intersect(l.Classes.SelectMany(c => c.LessonTimings)))
				.Where(predicate: t => !t.Class.EducationPeriodForClasses.Any(
					epfc => epfc.EducationPeriod.Holidays.Any(h => h.StartDate < d && h.EndDate > d)
				))
				.Where(predicate: t => (DayOfWeek)t.DayOfWeekId == d.DayOfWeek &&
					t.LessonId == request.SubjectId && t.ClassId == request.ClassId
				).OrderBy(keySelector: t => t.DayOfWeekId == 0 ? 7 : t.DayOfWeekId)
				.ThenBy(keySelector: t => t.Number)
				.Select(selector: t => new GetTimetableWithoutAssessmentsResponse(
					new Subject(t.LessonId, t.Number, t.Class.Name, t.Lesson.Name, d, t.StartTime, t.EndTime),
					null
				))
			);
		return Ok(value: timetable);
	}

	[HttpGet(template: "get")]
	[Authorize(Policy = nameof(UserRoles.Administrator))]
	public async Task<ActionResult<GetTimetableByClassResponse>> GetTimetableForGroup(
		[FromQuery] GetTimetableByClassRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		GetTimetableByClassResponse[] timings = await _context.DaysOfWeeks.AsNoTracking()
			.Select(selector: d => new GetTimetableByClassResponse(
				new DayOfWeekOnTimetable(d.Id, d.DayOfWeek),
				d.LessonTimings.Count(t => t.ClassId == request.ClassId),
				d.LessonTimings.Where(t => t.ClassId == request.ClassId)
					.Select(t => new TimetableInClass(
						new SubjectInClass(t.Number, t.Lesson.Name, t.StartTime, t.EndTime),
						null
					)).OrderBy(t => t.Subject.Number)
			)).ToArrayAsync(cancellationToken: cancellationToken);

		foreach (GetTimetableByClassResponse timing in timings)
		{
			for (int i = 0; i < timing.Timetable.Count(); ++i)
			{
				TimetableInClass? currentTiming = timing.Timetable.ElementAtOrDefault(index: i);
				TimetableInClass? nextTiming = timing.Timetable.ElementAtOrDefault(index: i + 1);
				if (nextTiming is not null)
					currentTiming!.Break = new Break(CountMinutes: (nextTiming.Subject.Start - currentTiming.Subject.End).TotalMinutes);
			}
		}

		return Ok(value: timings);
	}
	#endregion
	#endregion
}