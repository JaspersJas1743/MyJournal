using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetAllAssignedTasksRequestValidator : AbstractValidator<TaskController.GetAllAssignedTasksRequest>
{
	public GetAllAssignedTasksRequestValidator()
	{
		RuleFor(expression: request => request.CompletionStatus)
			.IsInEnum().WithMessage(errorMessage: "Идентификатор статуса выполнения имеет некорректный формат");

		RuleFor(expression: request => request.Offset)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Смещение для выборки сообщений не может быть отрицательным.");

		RuleFor(expression: request => request.Count)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Количество возвращаемых сообщений не может быть отрицательным.");
	}
}