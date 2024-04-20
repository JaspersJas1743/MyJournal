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
	private bool _haveError = false;

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
		Debug.WriteLine($"Registration code: {RegistrationCode}\n" +
								$"Login: {Login}\n" +
								$"Password: {Password}");
		// MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
		// 	newVMType: typeof(ThirdStepOfRegistrationVM),
		// 	directionOfTransitionAnimation: PageTransition.Direction.Right
		// ));
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.Password,
			isPropertyValid: password => password?.Length >= 6 && password == _confirmationPassword,
			message: "Минимальная длина пароля - 6 символов."
		);

		IObservable<bool> passwordsObservable = this.WhenAnyValue(
			property1: x => x.Password,
			property2: x => x.ConfirmationPassword,
			selector: (password, confirmation) => password == confirmation);

		this.ValidationRule(
			viewModelProperty: model => model.ConfirmationPassword,
			viewModelObservable: passwordsObservable,
			message: "Пароли не совпадают."
		);
	}
}