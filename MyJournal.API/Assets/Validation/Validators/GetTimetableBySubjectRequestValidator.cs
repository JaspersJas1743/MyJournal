using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetTimetableBySubjectRequestValidator : AbstractValidator<TimetableController.GetTimetableBySubjectRequest>
{
	public GetTimetableBySubjectRequestValidator()
	{
		RuleFor(expression: request => request.SubjectId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");
	}
}