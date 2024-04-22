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

public sealed class SeventhStepOfRegistrationViaPhoneModel : ModelWithErrorMessage
{
	private readonly IRegistrationService<User> _registrationService;

	private string _phone = String.Empty;

	public SeventhStepOfRegistrationViaPhoneModel(IRegistrationService<User> registrationService)
	{
		_registrationService = registrationService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public string Phone
	{
		get => _phone;
		set => this.RaiseAndSetIfChanged(backingField: ref _phone, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		try
		{
			await _registrationService.SetPhone(phone: Phone);
			MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
				newVMType: typeof(EndOfRegistrationVM),
				directionOfTransitionAnimation: PageTransition.Direction.Left
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
		this.ValidationRule(
			viewModelProperty: model => model.Phone,
			isPropertyValid: phone => Regex.IsMatch(input: phone, pattern: @"\+7\(\d{3}\)\d{3}-\d{4}"),
			message: "Неверный формат номера."
		);
	}
}