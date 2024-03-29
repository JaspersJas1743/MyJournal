using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetAssessmentsRequestValidator : AbstractValidator<AssessmentController.GetAssessmentsRequest>
{
	public GetAssessmentsRequestValidator()
	{
		RuleFor(expression: request => request.PeriodId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор периода не может быть отрицательным.");

		RuleFor(expression: request => request.SubjectId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");
	}
}