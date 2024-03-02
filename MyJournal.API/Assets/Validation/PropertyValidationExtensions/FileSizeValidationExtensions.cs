using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class FileSizeValidationExtensions
{
	public static IRuleBuilderOptions<T, IFormFile> WithSizeBetween<T>(
		this IRuleBuilderOptions<T, IFormFile> ruleBuilder,
		int minSize = 0,
		int maxSize = Int32.MaxValue
	)
	{
		return ruleBuilder.Must(predicate: file => file.Length >= minSize).WithMessage($"Размер файла не должен быть менее, чем {minSize} байт")
						  .Must(predicate: file => file.Length <= maxSize).WithMessage($"Размер файла не должен быть более, чем {maxSize / 1024 / 1024} Мбайт");
	}

	public static IRuleBuilderOptions<T, IFormFile> WithSizeBetween<T>(
		this IRuleBuilderInitial<T, IFormFile> ruleBuilder,
		int minSize = 0,
		int maxSize = Int32.MaxValue
	)
	{
		return ruleBuilder.Must(predicate: file => file.Length >= minSize).WithMessage($"Размер файла не должен быть менее, чем {minSize} байт")
						  .Must(predicate: file => file.Length <= maxSize).WithMessage($"Размер файла не должен быть более, чем {maxSize / 1024 / 1024} Мбайт");
	}
}