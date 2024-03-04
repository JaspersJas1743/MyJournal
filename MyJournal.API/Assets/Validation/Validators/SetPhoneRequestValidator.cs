using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public class SetPhoneRequestValidator : AbstractValidator<AccountController.SetPhoneRequest>
{
	public SetPhoneRequestValidator()
	{
		RuleFor(expression: request => request.NewPhone)
			.Cascade(cascadeMode: CascadeMode.Stop).Phone();
	}
}