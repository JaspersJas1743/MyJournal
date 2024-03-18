using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetDialogsRequestValidator : AbstractValidator<ChatController.GetChatsRequest>
{
	public GetDialogsRequestValidator()
	{
		When(predicate: request => request.IsFiltered, action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.HaveText(errorMessage: "Критерий для фильтрации диалогов не установлен, хотя IsFiltered является true.");
		}).Otherwise(action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.DontHaveText(errorMessage: "Критерий фильтрации не должен быть установлен, так как IsFiltered является false.");
		});

		RuleFor(expression: request => request.Offset)
			.GreaterThanOrEqualTo(valueToCompare: 0).WithMessage(errorMessage: "Смещение для выборки диалогов не может быть отрицательным.");

		RuleFor(expression: request => request.Count)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Количество возвращаемых диалогов должно быть больше 0.");
	}
}