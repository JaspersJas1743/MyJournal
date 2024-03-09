using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class IsUrlExtensions
{
	public static IRuleBuilderOptions<T, string?> IsUrl<T>(
		this IRuleBuilderOptions<T, string?> ruleBuilder
	) => ruleBuilder.Must(predicate: url => Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out _));

	public static IRuleBuilderOptions<T, string?> IsUrl<T>(
		this IRuleBuilderInitial<T, string?> ruleBuilder
	) => ruleBuilder.Must(predicate: url => Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, out _));
}