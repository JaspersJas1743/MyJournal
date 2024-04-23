using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.Assets.Utilities.MessagesService;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Registration;

public sealed class ThirdStepOfRegistrationModel : ValidatableModel
{
	private readonly IRegistrationService<User> _registrationService;
	private readonly IMessageService _messageService;

	private string _password = String.Empty;
	private string _confirmationPassword = String.Empty;

	public ThirdStepOfRegistrationModel(
		IRegistrationService<User> registrationService,
		IMessageService messageService
	)
	{
		_registrationService = registrationService;
		_messageService = messageService;

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
		bool registrationIsSuccessful = await _registrationService.Register(credentials: new UserCredentials()
		{
			Login = Login,
			Password = Password,
			RegistrationCode = RegistrationCode
		});
		if (!registrationIsSuccessful)
		{
			await _messageService.ShowErrorAsPopup(text: "В процессе регистрации произошла ошибка. Обратитесь к Вашей учебной организации.");
			return;
		}

		IRegistrationService<User>.AuthenticationData codes = await _registrationService.CreateGoogleAuthenticator();
		FourthStepOfRegistrationVM nextStep = (Application.Current as App)!.GetService<FourthStepOfRegistrationVM>();
		nextStep.QRCode = codes.QrCodeBase64;
		nextStep.Code = codes.AuthenticationCode;

		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVM: nextStep, animationType: AnimationType.DirectionToRight
		));
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