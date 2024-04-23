using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Core.Utilities;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Registration;

public sealed class SecondStepOfRegistrationModel : ModelWithErrorMessage
{
	private readonly IVerificationService<Credentials<User>> _verificationService;

	private string _login = String.Empty;

	public SecondStepOfRegistrationModel(
		[FromKeyedServices(key: nameof(LoginVerificationService))] IVerificationService<Credentials<User>> verificationService
	)
	{
		_verificationService = verificationService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public string RegistrationCode { private get; set; }

	public string Login
	{
		get => _login;
		set => this.RaiseAndSetIfChanged(backingField: ref _login, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		HaveError = !await _verificationService.Verify(toVerifying: new UserCredentials() { Login = Login });
		if (HaveError)
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		else
		{
			ThirdStepOfRegistrationVM newVM = (Application.Current as App)!.GetService<ThirdStepOfRegistrationVM>();
			newVM.SetData(login: Login, registrationCode: RegistrationCode);

			MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
				newVMType: typeof(ThirdStepOfRegistrationVM),
				directionOfTransitionAnimation: PageTransition.Direction.Left
			));
		}
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.Login,
			isPropertyValid: login => login?.Length >= 4,
			message: "Минимальная длина логина - 4 символа."
		);
	}
}