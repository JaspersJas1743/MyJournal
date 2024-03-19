using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class UploadProfilePhotoRequestValidator : AbstractValidator<UserController.UploadProfilePhotoRequest>
{
	public UploadProfilePhotoRequestValidator()
	{
		RuleFor(expression: request => request.Link)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveText(errorMessage: "Ссылка на файл имеет некорректный формат.")
			.AllowFileExtensions(extensions: new string[] { "jpg", "png", "jpeg" })
			.IsValidImageUrl().WithMessage(errorMessage: "Некорректная ссылка на файл.");
	}
}