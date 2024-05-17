using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetFinalAssessmentRequestValidator : AbstractValidator<AssessmentController.GetFinalAssessmentRequest>
{
	public GetFinalAssessmentRequestValidator()
	{
		RuleFor(expression: request => request.SubjectId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");

		RuleFor(expression: request => request.PeriodId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор учебного периода не может быть отрицательным.");
	}
}