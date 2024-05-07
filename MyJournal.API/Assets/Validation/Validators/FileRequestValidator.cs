using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class FileRequestValidator : AbstractValidator<FileController.FileRequest>
{
	public FileRequestValidator()
	{
		RuleFor(expression: request => request.File)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveContent(errorMessage: "Файл поврежден.")
			.WithSizeBetween(minSize: 1, maxSize: 31457280);
	}
}