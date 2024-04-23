using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.RestoringAccess;

public class ChangingPasswordWhenRestoringAccessModel : ModelWithErrorMessage
{
	private string _newPassword = String.Empty;
	private string _confirmationOfNewPassword = String.Empty;

	public ChangingPasswordWhenRestoringAccessModel()
	{
		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public IRestoringAccessService<User> RestoringAccessService { get; set; }

	public string NewPassword
	{
		get => _newPassword;
		set => this.RaiseAndSetIfChanged(backingField: ref _newPassword, newValue: value);
	}

	public string ConfirmationOfNewPassword
	{
		get => _confirmationOfNewPassword;
		set => this.RaiseAndSetIfChanged(backingField: ref _confirmationOfNewPassword, newValue: value);
	}

	public async Task MoveToNextStep()
	{
		try
		{
			await RestoringAccessService.ResetPassword(newPassword: NewPassword);

			// Переход на некст страницу
		}
		catch (Exception e)
		{
			Error = e.Message;
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		}
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.NewPassword,
			isPropertyValid: password => password?.Length >= 6,
			message: "Минимальная длина пароля - 6 символов."
		);

		IObservable<bool> passwordsObservable = this.WhenAnyValue(
			property1: model => model.NewPassword,
			property2: model => model.ConfirmationOfNewPassword,
			selector: (password, confirmation) => password == confirmation
		);

		this.ValidationRule(validationObservable: passwordsObservable, message: "Пароли не совпадают.");
	}
}