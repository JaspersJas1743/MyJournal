using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class SignUpRequestValidator : AbstractValidator<AccountController.SignUpRequest>
{
	public SignUpRequestValidator()
	{
		string errorMessage = "Регистрационный код имеет некорректный формат";
		RuleFor(expression: request => request.RegistrationCode)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.Must(predicate: code => !String.IsNullOrWhiteSpace(value: code)).WithMessage(errorMessage: errorMessage)
			.Length(exactLength: 7).WithMessage(errorMessage: "Длина регистрационного кода составляет 7 символов")
			.NotEmpty().WithMessage(errorMessage: errorMessage);

		errorMessage = "Логин имеет некорректный формат";
		RuleFor(expression: request => request.Login)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.Must(predicate: login => !String.IsNullOrWhiteSpace(value: login)).WithMessage(errorMessage: errorMessage)
			.MinimumLength(minimumLength: 4).WithMessage(errorMessage: "Минимальная длина логина составляет 4 символа")
			.NotEmpty().WithMessage(errorMessage: errorMessage);

		errorMessage = "Пароль имеет некорректный формат";
		RuleFor(expression: request => request.Password)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.Must(predicate: password => !String.IsNullOrWhiteSpace(value: password)).WithMessage(errorMessage: errorMessage)
			.MinimumLength(minimumLength: 6).WithMessage(errorMessage: "Минимальная длина пароля составляет 6 символов")
			.NotEmpty().WithMessage(errorMessage: errorMessage);
	}
}