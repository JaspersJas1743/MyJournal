using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class ChangePhoneRequestValidator : AbstractValidator<UserController.ChangePhoneRequest>
{
	public ChangePhoneRequestValidator()
	{
		RuleFor(expression: request => request.NewPhone)
			.Cascade(cascadeMode: CascadeMode.Stop).Phone();
	}
}