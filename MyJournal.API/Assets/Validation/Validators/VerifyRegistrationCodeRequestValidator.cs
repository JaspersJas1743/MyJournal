using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class VerifyRegistrationCodeRequestValidator : AbstractValidator<AccountController.VerifyRegistrationCodeRequest>
{
	public VerifyRegistrationCodeRequestValidator()
	{
		RuleFor(expression: request => request.RegistrationCode)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Регистрационный код имеет некорректный формат.")
			.Length(exactLength: 7).WithMessage(errorMessage: "Длина регистрационного кода составляет 7 символов.");
	}
}