using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class VerifyLoginRequestValidator : AbstractValidator<AccountController.VerifyLoginRequest>
{
	public VerifyLoginRequestValidator()
	{
		RuleFor(expression: request => request.Login)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Логин имеет некорректный формат.")
			.MinimumLength(minimumLength: 4).WithMessage(errorMessage: "Минимальная длина логина составляет 4 символа.");
	}
}