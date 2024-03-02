using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class FileContentValidationExtensions
{
	public static IRuleBuilderOptions<T, IFormFile> HaveContent<T>(
		this IRuleBuilderOptions<T, IFormFile> ruleBuilder
	) => ruleBuilder.NotNull().NotEmpty();

	public static IRuleBuilderOptions<T, IFormFile> HaveContent<T>(
		this IRuleBuilderInitial<T, IFormFile> ruleBuilder
	) => ruleBuilder.NotNull().NotEmpty();
}