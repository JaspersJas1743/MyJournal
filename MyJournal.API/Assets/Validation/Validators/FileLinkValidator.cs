using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class FileLinkValidator : AbstractValidator<FileController.FileLink>
{
	public FileLinkValidator()
	{
		RuleFor(expression: request => request.Link)
			.HaveText(errorMessage: "Ссылка на файл имеет некорректный формат.")
			.IsValidUrl().WithMessage(errorMessage: "Некорректная ссылка на файл.");
	}
}