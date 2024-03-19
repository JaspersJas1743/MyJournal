using FluentValidation;
using MyJournal.API.Assets.Controllers;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Validation.PropertyValidationExtensions;

namespace MyJournal.API.Assets.Validation.Validators;

public sealed class SendMessageRequestValidator : AbstractValidator<MessageController.SendMessageRequest>
{
	public SendMessageRequestValidator()
	{
		RuleFor(expression: request => request.ChatId)
			.GreaterThan(valueToCompare: 0).WithMessage(errorMessage: "Идентификатор чата не может быть отрицательным.");

		When(predicate: request => request.Content.Attachments is not null, action: () =>
		{
			RuleForEach(expression: request => request.Content.Attachments).ChildRules(action: r =>
			{
				r.RuleFor(expression: a => a.LinkToFile)
				 .HaveText(errorMessage: "Ссылка на фотографию имеет некорректный формат.")
				 .IsValidUrl().WithMessage(errorMessage: "Некорректная ссылка на вложение.");

				r.When(predicate: a => a.AttachmentType == AttachmentTypes.Photo, action: () =>
				{
					r.RuleFor(expression: a => a.LinkToFile)
					 .AllowFileExtensions(extensions: new string[] { "jpg", "png", "jpeg" })
					 .IsValidImageUrl().WithMessage(errorMessage: "Некорректная ссылка на изображение.");
				});

				r.RuleFor(expression: a => a.AttachmentType).IsInEnum();
			});
		}).Otherwise(action: () =>
			RuleFor(expression: request => request.Content.Text)
				.HaveText(errorMessage: "Text не может являться null, так как сообщение не содержит вложений.")
		);
	}
}