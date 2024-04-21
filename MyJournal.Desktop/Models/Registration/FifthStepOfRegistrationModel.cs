using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.Registration;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Registration;

public sealed class FifthStepOfRegistrationModel : ModelWithErrorMessage
{
	private readonly IRegistrationService<User> _registrationService;

	private string _entryCode = String.Empty;

	public FifthStepOfRegistrationModel(
		IRegistrationService<User> registrationService
	)
	{
		_registrationService = registrationService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public string EntryCode
	{
		get => _entryCode;
		set => this.RaiseAndSetIfChanged(backingField: ref _entryCode, newValue: value);
	}

	public int CountOfCell => 6;

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public async Task MoveToNextStep()
	{
		HaveError = !await _registrationService.VerifyAuthenticationCode(code: EntryCode);
		Debug.WriteLine($"HaveError: {HaveError}");
		if (HaveError)
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		else
		{
			// MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			// 	newVMType: typeof(SixStepOfRegistrationVM),
			// 	directionOfTransitionAnimation: PageTransition.Direction.Left
			// ));
		}
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.EntryCode,
			isPropertyValid: code => code?.Length == CountOfCell,
			message: "Регистрационный код имеет некорректный формат."
		);
	}
}