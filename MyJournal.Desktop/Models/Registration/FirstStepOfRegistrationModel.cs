using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Core.Utilities;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.ViewModels.Authorization;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public class FirstStepOfRegistrationModel : Drawable
{
	private readonly IVerificationService<Credentials<User>> _verificationService;

	private string _entryCode = String.Empty;
	private bool _haveError = false;

	public FirstStepOfRegistrationModel(IVerificationService<Credentials<User>> verificationService)
	{
		_verificationService = verificationService;

		ToNextStep = ReactiveCommand.CreateFromTask(
			execute: MoveToNextStep,
			canExecute: MoveToNextStepCanExecute()
		);
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

	public bool HasMoveToNextStep { get; private set; }

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public ReactiveCommand<Unit, Unit> ToAuthorization { get; }

	public async Task MoveToNextStep()
	{
		HaveError = !await _verificationService.Verify(toVerifying: new UserCredentials() { RegistrationCode = EntryCode });
		if (HaveError)
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		else
		{
			HasMoveToNextStep = true;
			MoveTo<SecondStepOfRegistrationVM>();
		}
	}

	public IObservable<bool> MoveToNextStepCanExecute()
	{
		return this.WhenAnyValue(property1: model => model.EntryCode)
			.Select(selector: code => code.Length == CodeInput.CountOfCell);
	}

	public void MoveToAuthorization()
		=> MoveTo<AuthorizationVM>();
}