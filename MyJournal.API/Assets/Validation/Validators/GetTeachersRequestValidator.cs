using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetTeachersRequestValidator : AbstractValidator<AdministratorController.GetTeachersRequest>
{
	public GetTeachersRequestValidator()
	{
		When(predicate: request => request.IsFiltered, action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.HaveText(errorMessage: "Критерий для фильтрации преподавателей не установлен.");
		}).Otherwise(action: () =>
		{
			RuleFor(expression: request => request.Filter)
				.Cascade(cascadeMode: CascadeMode.Stop)
				.DontHaveText(errorMessage: "Критерий фильтрации не должен быть установлен, так как IsFiltered установлено в false.");
		});
	}
}