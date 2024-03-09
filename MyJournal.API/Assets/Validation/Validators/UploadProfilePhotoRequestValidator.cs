using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class UploadProfilePhotoRequestValidator : AbstractValidator<UserController.UploadProfilePhotoRequest>
{
	public UploadProfilePhotoRequestValidator()
	{
		RuleFor(expression: request => request.Photo)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveContent().WithMessage(errorMessage: "Файл поврежден")
			.WithSizeBetween(minSize: 1, maxSize: 2097153)
			.AllowFileExtensions(extensions: new string[] { "jpg", "png", "jpeg" });
	}
}