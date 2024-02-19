using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class VerifyRegistrationCodeRequestValidator : AbstractValidator<AccountController.VerifyRegistrationCodeRequest>
{
	public VerifyRegistrationCodeRequestValidator()
	{
		string errorMessage = "Регистрационный код имеет некорректный формат";
		RuleFor(expression: request => request.RegistrationCode)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.Must(predicate: code => !String.IsNullOrWhiteSpace(value: code)).WithMessage(errorMessage: errorMessage)
			.Length(exactLength: 7).WithMessage(errorMessage: "Длина регистрационного кода составляет 7 символов")
			.NotEmpty().WithMessage(errorMessage: errorMessage);
	}
}