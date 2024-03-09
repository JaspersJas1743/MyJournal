using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class ResetPasswordRequestValidator : AbstractValidator<AccountController.ResetPasswordRequest>
{
	public ResetPasswordRequestValidator()
	{
		RuleFor(expression: request => request.NewPassword)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Пароль имеет некорректный формат.")
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов.");
	}
}