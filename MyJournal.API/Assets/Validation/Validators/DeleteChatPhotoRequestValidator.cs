using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class DeleteChatPhotoRequestValidator : AbstractValidator<ChatController.DeleteChatPhotoRequest>
{
	public DeleteChatPhotoRequestValidator()
	{
		RuleFor(expression: request => request.Link)
			.HaveText(errorMessage: "Ссылка на фотографию имеет некорректный формат.")
			.AllowFileExtensions(extensions: new string[] { "jpg", "png", "jpeg" })
			.IsValidImageUrl().WithMessage(errorMessage: "Некорректная ссылка на изображение.");
	}
}