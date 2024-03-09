using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<UserController.ChangePasswordRequest>
{
	public ChangePasswordRequestValidator()
	{
		RuleFor(expression: request => request.CurrentPassword)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Пароль имеет некорректный формат.")
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов.")
			.NotEqual(expression: request => request.CurrentPassword).WithMessage("Новый пароль должен отличаться от предыдущего.");
	}
}