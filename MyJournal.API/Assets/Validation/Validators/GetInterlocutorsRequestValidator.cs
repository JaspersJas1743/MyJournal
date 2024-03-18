using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetInterlocutorsRequestValidator : AbstractValidator<ChatController.GetInterlocutorsRequest>
{
	public GetInterlocutorsRequestValidator()
	{
		When(predicate: request => request.IsFiltered, action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.HaveText(errorMessage: "Критерий для фильтрации потенциальных собеседников не установлен.");
		}).Otherwise(action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.DontHaveText(errorMessage: "Критерий фильтрации не должен быть установлен, так как IsFiltered установлено в false.");
		});

		RuleFor(expression: request => request.Offset)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Смещение для выборки потенциальных собеседников не может быть отрицательным.");

		RuleFor(expression: request => request.Count)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Количество возвращаемых сообщений не может быть отрицательным.");
	}
}