using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class SetEmailRequestValidator : AbstractValidator<AccountController.SetEmailRequest>
{
	public SetEmailRequestValidator()
	{
		RuleFor(expression: request => request.NewEmail)
			.Cascade(cascadeMode: CascadeMode.Stop).Email();
	}
}