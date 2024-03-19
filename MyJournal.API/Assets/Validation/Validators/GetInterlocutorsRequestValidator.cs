using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetInterlocutorsRequestValidator : AbstractValidator<ChatController.GetInterlocutorsRequest>
{
	public GetInterlocutorsRequestValidator()
	{
		RuleFor(expression: request => request.Offset)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Смещение для выборки потенциальных собеседников не может быть отрицательным.");

		RuleFor(expression: request => request.Count)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Количество потенциальных собеседников не может быть отрицательным.");
	}
}