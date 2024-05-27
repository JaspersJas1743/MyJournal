using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class CreateAdministratorRequestValidator : AbstractValidator<AdministratorController.CreateAdministratorRequest>
{
	public CreateAdministratorRequestValidator()
	{
		RuleFor(expression: request => request.Surname)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Не указана фамилия пользователя")
			.MaximumLength(maximumLength: 20).WithMessage(errorMessage: "Максимальная длина фамилии пользователя - 20 символов.");

		RuleFor(expression: request => request.Name)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Не указано имя пользователя")
			.MaximumLength(maximumLength: 20).WithMessage(errorMessage: "Максимальная длина имени пользователя - 20 символов.");

		When(predicate: request => !String.IsNullOrEmpty(value: request.Patronymic), action: () =>
		{
			RuleFor(expression: request => request.Patronymic)
				.MaximumLength(maximumLength: 20).WithMessage(errorMessage: "Максимальная длина отчества пользователя - 20 символов.");
		});
	}
}