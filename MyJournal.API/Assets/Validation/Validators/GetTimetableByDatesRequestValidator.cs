using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetTimetableByDatesRequestValidator : AbstractValidator<TimetableController.GetTimetableByDatesRequest>
{
	public GetTimetableByDatesRequestValidator()
	{
		int currentYear = DateTime.Now.Year;
		RuleFor(expression: request => request.Days)
			.AllElementsInCollection(predicate: d => currentYear - 1 <= d.Year && currentYear + 1 >= d.Year)
			.WithMessage(errorMessage: "Одна из указанных дат является некорректной.");
	}
}