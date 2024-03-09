using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class SignInRequestValidator : AbstractValidator<AccountController.SignInRequest>
{
	public SignInRequestValidator()
	{
		RuleFor(expression: request => request.Login)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Логин имеет некорректный формат.")
			.MinimumLength(minimumLength: 4).WithMessage(errorMessage: "Минимальная длина логина составляет 4 символа.");

		RuleFor(expression: request => request.Password)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Пароль имеет некорректный формат.")
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов.");

		RuleFor(expression: request => request.Client)
			.IsInEnum().WithMessage("Идентификатор клиента имеет некорректный формат.");
	}
}