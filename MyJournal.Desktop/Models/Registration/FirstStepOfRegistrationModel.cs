using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Core.Utilities;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Authorization;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Registration;

public class FirstStepOfRegistrationModel : ValidatableModel
{
	private readonly IVerificationService<Credentials<User>> _verificationService;

	private string _entryCode = String.Empty;
	private bool _haveError = false;

	public FirstStepOfRegistrationModel(
		[FromKeyedServices(key: nameof(RegistrationCodeVerificationService))] IVerificationService<Credentials<User>> verificationService
	)
	{
		_verificationService = verificationService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
		ToAuthorization = ReactiveCommand.Create(execute: MoveToAuthorization);
	}

	public string EntryCode
	{
		get => _entryCode;
		set => this.RaiseAndSetIfChanged(backingField: ref _entryCode, newValue: value);
	}

	public bool HaveError
	{
		get => _haveError;
		set => this.RaiseAndSetIfChanged(backingField: ref _haveError, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public ReactiveCommand<Unit, Unit> ToAuthorization { get; }

	public async Task MoveToNextStep()
	{
		HaveError = !await _verificationService.Verify(toVerifying: new UserCredentials() { RegistrationCode = EntryCode });
		if (HaveError)
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		else
		{
			SecondStepOfRegistrationVM newVM = (Application.Current as App)!.GetService<SecondStepOfRegistrationVM>();
			newVM.SetRegistrationCode(code: EntryCode);

			MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
				newVM: newVM, directionOfTransitionAnimation: PageTransition.Direction.Left
			));
		}
	}

	public void MoveToAuthorization()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(AuthorizationVM),
			directionOfTransitionAnimation: PageTransition.Direction.Right
		));
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.EntryCode,
			isPropertyValid: code => code?.Length == CodeInput.CountOfCell,
			message: "Регистрационный код имеет некорректный формат."
		);
	}
}