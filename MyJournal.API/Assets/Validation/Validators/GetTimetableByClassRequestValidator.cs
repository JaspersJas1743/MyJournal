using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetTimetableByClassRequestValidator : AbstractValidator<TimetableController.GetTimetableByClassRequest>
{
	public GetTimetableByClassRequestValidator()
	{
		RuleFor(expression: request => request.ClassId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор класса не может быть отрицательным.");
	}
}