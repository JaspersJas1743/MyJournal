using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class CreateMultiChatRequestValidator : AbstractValidator<ChatController.CreateMultiChatRequest>
{
	public CreateMultiChatRequestValidator()
	{
		RuleFor(expression: request => request.InterlocutorIds)
			.Cascade(cascadeMode: CascadeMode.Stop)
			.AllElementsInCollection(predicate: Int32.IsPositive).WithMessage("Идентификатор собеседника должен быть больше 0.")
			.IsUniqueCollection().WithMessage("Идентификатор собеседника должен быть уникальным.");

		RuleFor(expression: request => request.ChatName)
			.HaveText(errorMessage: "Наименование чата имеет некорректный формат.");

		RuleFor(expression: request => request.LinkToPhoto)
			.HaveText(errorMessage: "Ссылка на фотографию имеет некорректный формат.")
			.AllowFileExtensions(extensions: new string[] { "jpg", "png", "jpeg" })
			.IsValidImageUrl().WithMessage(errorMessage: "Некорректная ссылка на изображение.");
	}
}