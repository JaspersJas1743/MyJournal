using FluentValidation;
using FluentValidation.Validators;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class EmailValidationExtensions
{
	public static IRuleBuilderOptions<T, string> Email<T>(
		this IRuleBuilderOptions<T, string> ruleBuilder
	) => ruleBuilder.EmailAddress(mode: EmailValidationMode.Net4xRegex).WithMessage("Некорректный формат электронной почты.");

	public static IRuleBuilderOptions<T, string> Email<T>(
		this IRuleBuilderInitial<T, string> ruleBuilder
	) => ruleBuilder.EmailAddress(mode: EmailValidationMode.Net4xRegex).WithMessage("Некорректный формат электронной почты.");
}