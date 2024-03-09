using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class FileContentValidationExtensions
{
	public static IRuleBuilderOptions<T, IFormFile> HaveContent<T>(
		this IRuleBuilderOptions<T, IFormFile> ruleBuilder,
		string errorMessage
	)
	{
		return ruleBuilder
			   .NotNull().WithMessage(errorMessage: errorMessage)
			   .NotEmpty().WithMessage(errorMessage: errorMessage);
	}

	public static IRuleBuilderOptions<T, IFormFile> HaveContent<T>(
		this IRuleBuilderInitial<T, IFormFile> ruleBuilder,
		string errorMessage
	)
	{
		return ruleBuilder
			   .NotNull().WithMessage(errorMessage: errorMessage)
			   .NotEmpty().WithMessage(errorMessage: errorMessage);
	}
}