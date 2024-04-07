using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetTimetableBySubjectAndClassRequestValidator : AbstractValidator<TimetableController.GetTimetableBySubjectAndClassRequest>
{
	public GetTimetableBySubjectAndClassRequestValidator()
	{
		RuleFor(expression: request => request.SubjectId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");

		RuleFor(expression: request => request.ClassId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор класса не может быть отрицательным.");
	}
}