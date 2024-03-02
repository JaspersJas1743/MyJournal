using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class AllowFileExtensionsValidationExtensions
{
	public static IRuleBuilderOptions<T, IFormFile> AllowFileExtensions<T>(
		this IRuleBuilderOptions<T, IFormFile> ruleBuilder,
		params string[] extensions
	)
	{
		return ruleBuilder.Must(predicate: file => extensions.Contains(value: Path.GetExtension(path: file.FileName).Trim(trimChar: '.')))
			.WithMessage("Файл имеет неподходящее расширение");
	}

	public static IRuleBuilderOptions<T, IFormFile> AllowFileExtensions<T>(
		this IRuleBuilderInitial<T, IFormFile> ruleBuilder,
		params string[] extensions
	)
	{
		return ruleBuilder.Must(predicate: file => extensions.Contains(value: Path.GetExtension(path: file.FileName).Trim(trimChar: '.')))
			.WithMessage("Файл имеет неподходящее расширение");
	}
}