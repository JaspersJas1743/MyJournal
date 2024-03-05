using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<AccountController.ResetPasswordRequest>
{
	public ResetPasswordRequestValidator()
	{
		string errorMessage = "Пароль имеет некорректный формат.";
		RuleFor(expression: request => request.NewPassword)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.Must(predicate: password => !String.IsNullOrWhiteSpace(value: password)).WithMessage(errorMessage: errorMessage)
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов.")
			.NotEmpty().WithMessage(errorMessage: errorMessage);
	}
}