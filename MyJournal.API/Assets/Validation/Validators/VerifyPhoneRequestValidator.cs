using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public class VerifyPhoneRequestValidator : AbstractValidator<AccountController.VerifyPhoneRequest>
{
	public VerifyPhoneRequestValidator()
	{
		RuleFor(expression: request => request.Phone)
			.Cascade(cascadeMode: CascadeMode.Stop).Phone();
	}
}