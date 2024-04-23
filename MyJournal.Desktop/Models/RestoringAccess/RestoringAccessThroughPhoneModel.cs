using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.RestoringAccess;

public class RestoringAccessThroughPhoneModel : ModelWithErrorMessage
{
	private readonly IRestoringAccessService<User> _restoringAccessService;

	private string _phone = String.Empty;

	public RestoringAccessThroughPhoneModel(
		[FromKeyedServices(key: nameof(RestoringAccessThroughPhoneService))] IRestoringAccessService<User> restoringAccessService
	)
	{
		_restoringAccessService = restoringAccessService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
		ToRestoringAccessThroughEmail = ReactiveCommand.Create(execute: MoveToRestoringAccessThroughEmail);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public ReactiveCommand<Unit, Unit> ToRestoringAccessThroughEmail { get; }

	public string Phone
	{
		get => _phone;
		set => this.RaiseAndSetIfChanged(backingField: ref _phone, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		VerificationResult result = await _restoringAccessService.VerifyCredential(credentials: new PhoneCredentials() { Phone = Phone });
		Error = result.ErrorMessage;
		if (HaveError)
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		else
		{
			ConfirmationOfRestoringAccessVM newVM = (Application.Current as App)!.GetService<ConfirmationOfRestoringAccessVM>();
			newVM.RestoringAccessService = _restoringAccessService;

			MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
				newVM: newVM, animationType: AnimationType.DirectionToRight
			));
		}
	}

	public void MoveToRestoringAccessThroughEmail()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(RestoringAccessThroughEmailVM),
			animationType: AnimationType.CrossFade
		));
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.Phone,
			isPropertyValid: phone => Regex.IsMatch(input: phone, pattern: @"\+7\(\d{3}\)\d{3}-\d{4}"),
			message: "Неверный формат адреса электронной почты."
		);
	}
}