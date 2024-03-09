using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class UploadChatPhotoRequestValidator : AbstractValidator<ChatsController.UploadChatPhotoRequest>
{
	public UploadChatPhotoRequestValidator()
	{
		RuleFor(expression: request => request.Photo)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.HaveContent(errorMessage: "Файл поврежден.")
			.WithSizeBetween(minSize: 1, maxSize: 2097153)
			.AllowFileExtensions(extensions: new string[] { "jpg", "png", "jpeg" });
	}
}