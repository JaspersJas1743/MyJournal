using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Utilities.SubjectComparers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class CreateTimetableRequestValidator : AbstractValidator<TimetableController.CreateTimetableRequest>
{
	private static readonly SubjectNumberComparer _numberComparer = new SubjectNumberComparer();
	private static readonly SubjectStartTimeComparer _startTimeComparer = new SubjectStartTimeComparer();
	private static readonly SubjectEndTimeComparer _endTimeComparer = new SubjectEndTimeComparer();

	public CreateTimetableRequestValidator()
	{
		RuleFor(expression: request => request.ClassId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор класса не может быть отрицательным.");

		RuleForEach(expression: request => request.Timetable).ChildRules(action: schedule =>
		{
			schedule.RuleFor(expression: s => s.DayOfWeekId)
				.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дня недели не может быть отрицательным.");

			schedule.RuleFor(expression: s => s.Subjects)
					.IsUniqueCollection(comparer: _numberComparer).WithMessage(errorMessage: "Несколько дисциплин имеют одинаковый порядковый номер.");

			schedule.RuleFor(expression: s => s.Subjects)
					.IsUniqueCollection(comparer: _startTimeComparer).WithMessage(errorMessage: "Несколько дисциплин начинаются в одно время.");

			schedule.RuleFor(expression: s => s.Subjects)
					.IsUniqueCollection(comparer: _endTimeComparer).WithMessage(errorMessage: "Несколько дисциплин заканчиваются в одно время.");

			schedule.RuleForEach(expression: s => s.Subjects).ChildRules(action: subject =>
			{
				subject.RuleFor(expression: s => s.Id)
					.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");

				subject.RuleFor(expression: s => s.Number)
					.GreaterThanOrEqualTo(valueToCompare: 1).WithMessage(errorMessage: "Номера занятий должны начинаться с 1.")
					.LessThanOrEqualTo(valueToCompare: 8).WithMessage(errorMessage: "В день не может быть больше 8 занятий.");

				subject.RuleFor(expression: s => s.Start)
					.GreaterThanOrEqualTo(valueToCompare: TimeSpan.FromHours(value: 9)).WithMessage(errorMessage: "Занятия не могут начинаться раньше 9:00");

				subject.RuleFor(expression: s => s.End)
					.LessThanOrEqualTo(valueToCompare: TimeSpan.FromHours(value: 17)).WithMessage(errorMessage: "Занятия не могут заканчиваться позднее 17:00");

				subject.RuleFor(expression: s => s.End)
					.GreaterThan(expression: s => s.Start).WithMessage(errorMessage: "Занятие не может заканчиваться до его начала.");
			});
		});
	}
}