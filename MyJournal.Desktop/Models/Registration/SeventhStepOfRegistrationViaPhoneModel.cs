using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public sealed class SeventhStepOfRegistrationViaPhoneModel : ModelWithErrorMessage
{
	public SeventhStepOfRegistrationViaPhoneModel()
	{
		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public async Task MoveToNextStep()
	{

	}

	protected override void SetValidationRule()
	{

	}
}