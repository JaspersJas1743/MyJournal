using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Registration;

public sealed class SeventhStepOfRegistrationViaEmailModel : ModelWithErrorMessage
{
	private readonly IRegistrationService<User> _registrationService;

	private string _email = String.Empty;

	public SeventhStepOfRegistrationViaEmailModel(IRegistrationService<User> registrationService)
	{
		_registrationService = registrationService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public string Email
	{
		get => _email;
		set => this.RaiseAndSetIfChanged(backingField: ref _email, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		try
		{
			await _registrationService.SetEmail(email: Email);
			MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
				newVMType: typeof(EndOfRegistrationVM),
				animationType: AnimationType.DirectionToRight
			));
		}
		catch (ApiException e)
		{
			Error = e.Message;
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		}
	}

	protected override void SetValidationRule()
	{
		const string expression = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$";
		this.ValidationRule(
			viewModelProperty: model => model.Email,
			isPropertyValid: phone => Regex.IsMatch(input: phone, pattern: expression),
			message: "Неверный формат адреса электронной почты."
		);
	}
}