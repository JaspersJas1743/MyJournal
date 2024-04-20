using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Registration;

public sealed class ThirdStepOfRegistrationModel : ValidatableModel
{
	private string _password = String.Empty;
	private string _confirmationPassword = String.Empty;

	public ThirdStepOfRegistrationModel()
	{
		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public string RegistrationCode { private get; set; }
	public string Login { private get; set; }

	public string Password
	{
		get => _password;
		set => this.RaiseAndSetIfChanged(backingField: ref _password, newValue: value);
	}

	public string ConfirmationPassword
	{
		get => _confirmationPassword;
		set => this.RaiseAndSetIfChanged(backingField: ref _confirmationPassword, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		// MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
		// 	newVMType: typeof(ThirdStepOfRegistrationVM),
		// 	directionOfTransitionAnimation: PageTransition.Direction.Right
		// ));
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.Password,
			isPropertyValid: password => password?.Length >= 6,
			message: "Минимальная длина пароля - 6 символов."
		);

		IObservable<bool> passwordsObservable = this.WhenAnyValue(
			property1: model => model.Password,
			property2: model => model.ConfirmationPassword,
			selector: (password, confirmation) => password == confirmation
		);

		this.ValidationRule(validationObservable: passwordsObservable, message: "Пароли не совпадают.");
	}
}