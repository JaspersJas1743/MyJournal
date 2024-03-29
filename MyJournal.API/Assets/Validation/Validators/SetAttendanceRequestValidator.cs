using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class SetAttendanceRequestValidator : AbstractValidator<AssessmentController.SetAttendanceRequest>
{
	public SetAttendanceRequestValidator()
	{
		RuleFor(expression: request => request.SubjectId)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины не может быть отрицательным.");

		RuleForEach(expression: request => request.Attendances).ChildRules(action: attendance =>
		{
			attendance.RuleFor(expression: a => a.StudentId)
				.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор ученика не может быть отрицательным.");

			attendance.When(predicate: a => a.IsPresent, action: () =>
			{
				attendance.RuleFor(expression: a => a.CommentId)
					.Null().WithMessage(errorMessage: "Если ученик присутствует, то комментарий должен быть null.");
			}).Otherwise(action: () =>
			{
				attendance.RuleFor(expression: a => a.CommentId)
					.NotNull().WithMessage(errorMessage: "Если ученик отсутствует, то комментарий не может быть null.");
			});
		});
	}
}