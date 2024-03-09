using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public class AccountControllerVerifyGoogleAuthenticatorRequestValidator : AbstractValidator<AccountController.VerifyGoogleAuthenticatorRequest>
{
	public AccountControllerVerifyGoogleAuthenticatorRequestValidator()
	{
		RuleFor(expression: request => request.UserCode)
			.Length(exactLength: 6).WithMessage(errorMessage: "Длина кода подтверждения составляет 6 символов.");
	}
}