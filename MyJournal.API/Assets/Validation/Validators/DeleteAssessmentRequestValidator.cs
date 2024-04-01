using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class DeleteAssessmentRequestValidator : AbstractValidator<AssessmentController.DeleteAssessmentRequest>
{
	public DeleteAssessmentRequestValidator()
	{
		RuleFor(expression: request => request.AssessmentId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор отметки не может быть отрицательным.");
	}
}