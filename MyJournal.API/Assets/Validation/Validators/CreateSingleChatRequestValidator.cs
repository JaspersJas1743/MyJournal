using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class CreateSingleChatRequestValidator : AbstractValidator<ChatsController.CreateSingleChatRequest>
{
	public CreateSingleChatRequestValidator()
	{
		RuleFor(expression: request => request.InterlocutorId)
			.GreaterThan(valueToCompare: 0).WithMessage("Идентификатор собеседника должен быть больше 0.");
	}
}