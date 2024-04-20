using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;

namespace MyJournal.Desktop.Models;

public abstract class ValidatableModel : ModelBase, IValidatableViewModel
{
	public ValidatableModel()
		=> SetValidationRule();

	public ValidationContext ValidationContext { get; } = new ValidationContext();

	protected abstract void SetValidationRule();
}