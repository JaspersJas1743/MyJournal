using FluentValidation;
using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class GetTimetableByDateRequestValidator : AbstractValidator<TimetableController.GetTimetableByDateRequest>
{
	public GetTimetableByDateRequestValidator()
	{
		int currentYear = DateTime.Now.Year;
		RuleFor(expression: request => request.Day)
			.Must(predicate: d => currentYear - 1 <= d.Year && currentYear + 1 >= d.Year).WithMessage(errorMessage: "Указанная дата является некорректной.");
	}
}