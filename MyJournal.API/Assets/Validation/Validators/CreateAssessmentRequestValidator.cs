using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class CreateAssessmentRequestValidator : AbstractValidator<AssessmentController.CreateAssessmentRequest>
{
	public CreateAssessmentRequestValidator()
	{
		RuleFor(expression: request => request.GradeId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор оценки не может быть отрицательным.");

		RuleFor(expression: request => request.CommentId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор комментария не может быть отрицательным.");

		RuleFor(expression: request => request.SubjectId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");

		RuleFor(expression: request => request.StudentId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор ученика не может быть отрицательным.");
	}
}