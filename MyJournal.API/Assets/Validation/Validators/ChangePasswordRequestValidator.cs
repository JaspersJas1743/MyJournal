using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<UserController.ChangePasswordRequest>
{
	public ChangePasswordRequestValidator()
	{
		string errorMessage = "Пароль имеет некорректный формат.";
		RuleFor(expression: request => request.CurrentPassword)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.Must(predicate: password => !String.IsNullOrWhiteSpace(value: password)).WithMessage(errorMessage: errorMessage)
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов.")
			.NotEqual(expression: request => request.CurrentPassword).WithMessage("Новый пароль должен отличаться от предыдущего.")
			.NotEmpty().WithMessage(errorMessage: errorMessage);
	}
}