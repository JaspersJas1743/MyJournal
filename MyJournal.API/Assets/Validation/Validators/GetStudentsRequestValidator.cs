using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetStudentsRequestValidator : AbstractValidator<AdministratorController.GetStudentsRequest>
{
	public GetStudentsRequestValidator()
	{
		When(predicate: request => request.IsFiltered, action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.HaveText(errorMessage: "Критерий для фильтрации обучающихся не установлен.");
		}).Otherwise(action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.DontHaveText(errorMessage: "Критерий фильтрации не должен быть установлен, так как IsFiltered установлено в false.");
		});
	}
}