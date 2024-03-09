using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class SignUpRequestValidator : AbstractValidator<AccountController.SignUpRequest>
{
	public SignUpRequestValidator()
	{
		RuleFor(expression: request => request.RegistrationCode)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Регистрационный код имеет некорректный формат.")
			.Length(exactLength: 7).WithMessage(errorMessage: "Длина регистрационного кода составляет 7 символов.");

		RuleFor(expression: request => request.Login)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Логин имеет некорректный формат.")
			.MinimumLength(minimumLength: 4).WithMessage(errorMessage: "Минимальная длина логина составляет 4 символа.");

		RuleFor(expression: request => request.Password)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Пароль имеет некорректный формат.")
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов.");
	}
}