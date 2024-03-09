using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class HaveTextExtensions
{
	public static IRuleBuilderOptions<T, string?> HaveText<T>(
		this IRuleBuilderOptions<T, string?> ruleBuilder,
		string errorMessage
	)
	{
		return ruleBuilder
			.Must(predicate: text => !String.IsNullOrWhiteSpace(value: text)).WithMessage(errorMessage)
			.NotEmpty().WithMessage(errorMessage);
	}

	public static IRuleBuilderOptions<T, string?> HaveText<T>(
		this IRuleBuilderInitial<T, string?> ruleBuilder,
		string errorMessage
	)
	{
		return ruleBuilder
			.Must(predicate: text => !String.IsNullOrWhiteSpace(value: text)).WithMessage(errorMessage)
			.NotEmpty().WithMessage(errorMessage);
	}

	public static IRuleBuilderOptions<T, string?> DontHaveText<T>(
		this IRuleBuilderOptions<T, string?> ruleBuilder,
		string errorMessage
	)
	{
		return ruleBuilder
			.Must(predicate: String.IsNullOrWhiteSpace).WithMessage(errorMessage)
			.Empty().WithMessage(errorMessage);
	}

	public static IRuleBuilderOptions<T, string?> DontHaveText<T>(
		this IRuleBuilderInitial<T, string?> ruleBuilder,
		string errorMessage
	)
	{
		return ruleBuilder
			.Must(predicate: String.IsNullOrWhiteSpace).WithMessage(errorMessage)
			.Empty().WithMessage(errorMessage);
	}
}