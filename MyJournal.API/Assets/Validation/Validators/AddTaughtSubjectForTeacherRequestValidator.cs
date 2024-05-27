using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class AddTaughtSubjectForTeacherRequestValidator : AbstractValidator<AdministratorController.AddTaughtSubjectForTeacherRequest>
{
	public AddTaughtSubjectForTeacherRequestValidator()
	{
		RuleFor(expression: request => request.SubjectId)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор дисциплины должен быть больше 0.");

		RuleFor(expression: request => request.TeacherId)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор преподавателя должен быть больше 0.");

		RuleFor(expression: request => request.ClassId)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор учебного класса должен быть больше 0.");
	}
}