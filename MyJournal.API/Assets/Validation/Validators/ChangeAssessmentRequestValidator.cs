using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class ChangeAssessmentRequestValidator : AbstractValidator<AssessmentController.ChangeAssessmentRequest>
{
	public ChangeAssessmentRequestValidator()
	{
		RuleFor(expression: request => request.ChangedAssessmentId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор отметки не может быть отрицательным.");

		RuleFor(expression: request => request.NewAssessmentId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор отметки не может быть отрицательным.");

		RuleFor(expression: request => request.CommentId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор комментария не может быть отрицательным.");
	}
}