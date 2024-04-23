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

public class RestoringAccessThroughEmailModel : ModelWithErrorMessage
{
	private readonly IRestoringAccessService<User> _restoringAccessService;

	private string _email = String.Empty;

	public RestoringAccessThroughEmailModel(
		[FromKeyedServices(key: nameof(RestoringAccessThroughEmailService))] IRestoringAccessService<User> restoringAccessService
	)
	{
		_restoringAccessService = restoringAccessService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
		ToRestoringAccessThroughPhone = ReactiveCommand.Create(execute: MoveToRestoringAccessThroughPhone);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public ReactiveCommand<Unit, Unit> ToRestoringAccessThroughPhone { get; }

	public string Email
	{
		get => _email;
		set => this.RaiseAndSetIfChanged(backingField: ref _email, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		VerificationResult result = await _restoringAccessService.VerifyCredential(credentials: new EmailCredentials() { Email = Email});
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

	public void MoveToRestoringAccessThroughPhone()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(RestoringAccessThroughPhoneVM),
			animationType: AnimationType.CrossFade
		));
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