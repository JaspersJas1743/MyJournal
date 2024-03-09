using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class VerifyEmailRequestValidator : AbstractValidator<AccountController.VerifyEmailRequest>
{
	public VerifyEmailRequestValidator()
	{
		RuleFor(expression: request => request.Email)
			.Cascade(cascadeMode: CascadeMode.Stop).Email();
	}
}