using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetMessagesRequestValidator : AbstractValidator<MessageController.GetMessagesRequest>
{
	public GetMessagesRequestValidator()
	{
		RuleFor(expression: request => request.ChatId).GreaterThan(valueToCompare: 0)
			.WithMessage(errorMessage: "Идентификатор чата не может быть отрицательным.");

		RuleFor(expression: request => request.Offset)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Смещение для выборки сообщений не может быть отрицательным.");

		RuleFor(expression: request => request.Count)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Количество возвращаемых сообщений не может быть отрицательным.");
	}
}