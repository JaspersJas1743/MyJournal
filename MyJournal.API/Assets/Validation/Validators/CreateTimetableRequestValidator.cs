using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class CreateTimetableRequestValidator : AbstractValidator<TimetableController.CreateTimetableRequest>
{
	public CreateTimetableRequestValidator()
	{
		RuleFor(expression: request => request.ClassId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор класса не может быть отрицательным.");

		RuleForEach(expression: request => request.Timetable).ChildRules(action: shedule =>
		{
			shedule.RuleFor(expression: s => s.DayOfWeekId)
				.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дня недели не может быть отрицательным.");

			shedule.RuleForEach(expression: s => s.Subjects).ChildRules(action: subject =>
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
			});
		});
	}
}