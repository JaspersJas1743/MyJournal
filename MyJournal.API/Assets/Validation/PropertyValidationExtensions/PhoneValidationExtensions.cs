using System.Text.RegularExpressions;
using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class PhoneValidationExtensions
{
	public static IRuleBuilderOptions<T, string> Phone<T>(
		this IRuleBuilderOptions<T, string> ruleBuilder
	)
	{
		return ruleBuilder.HaveText(errorMessage: "Номер телефона имеет некорректный формат.")
						  .Length(exactLength: 15).WithMessage(errorMessage: "Длина номера телефона должна составлять 15 символов.")
						  .Matches(regex: new Regex(pattern: @"\+7\(\d{3}\)\d{3}-\d{4}")).WithMessage(errorMessage: "Номер телефона должен иметь формат +7(###)###-####.");
	}

	public static IRuleBuilderOptions<T, string> Phone<T>(
		this IRuleBuilderInitial<T, string> ruleBuilder
	)
	{
		return ruleBuilder.HaveText(errorMessage: "Номер телефона имеет некорректный формат.")
						  .Length(exactLength: 15).WithMessage(errorMessage: "Длина номера телефона должна составлять 15 символов.")
						  .Matches(regex: new Regex(pattern: @"\+7\(\d{3}\)\d{3}-\d{4}")).WithMessage(errorMessage: "Номер телефона должен иметь формат +7(###)###-####.");
	}
}