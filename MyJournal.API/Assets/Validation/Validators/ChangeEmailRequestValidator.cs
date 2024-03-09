using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class ChangeEmailRequestValidator : AbstractValidator<UserController.ChangeEmailRequest>
{
	public ChangeEmailRequestValidator()
	{
		RuleFor(expression: request => request.NewEmail)
			.Cascade(cascadeMode: CascadeMode.Stop).Email();
	}
}