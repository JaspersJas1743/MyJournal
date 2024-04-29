using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ConfirmationService;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileSecurityModel : ModelWithErrorMessage
{
	private const string ConfirmationMessage = """
											   Для изменения пароля от учетной записи необходимо
											   подтвердить, что Вы - владелец аккаунта. Для этого
											   откройте приложение Google Authenticator и
											   введите код для MyJournal
											   """;

	private readonly IConfirmationService _confirmationService;

	private Password _password;
	private string _currentPassword;
	private string _newPassword;
	private string _newPasswordConfirmation;

	public ProfileSecurityModel(IConfirmationService confirmationService)
	{
		_confirmationService = confirmationService;

		ChangePassword = ReactiveCommand.CreateFromTask(execute: ChangeUserPassword, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ChangePassword { get; }

	public string CurrentPassword
	{
		get => _currentPassword;
		set => this.RaiseAndSetIfChanged(backingField: ref _currentPassword, newValue: value);
	}

	public string NewPassword
	{
		get => _newPassword;
		set => this.RaiseAndSetIfChanged(backingField: ref _newPassword, newValue: value);
	}

	public string NewPasswordConfirmation
	{
		get => _newPasswordConfirmation;
		set => this.RaiseAndSetIfChanged(backingField: ref _newPasswordConfirmation, newValue: value);
	}

	private async Task ChangeUserPassword()
	{
		await _confirmationService.Сonfirm(text: ConfirmationMessage, command: ReactiveCommand.CreateFromTask(execute: async (string code) =>
		{
			try
			{
				string message = await _password.Change(confirmationCode: code, currentPassword: CurrentPassword, newPassword: NewPassword);
				return new CommandExecuteResult(ExecuteResult: CommandExecuteResults.Confirmed, Message: message);
			}
			catch (ArgumentException ex)
			{
				return new CommandExecuteResult(ExecuteResult: CommandExecuteResults.Unconfirmed, Message: ex.Message);
			}
			catch (ApiException ex)
			{
				Error = ex.Message;
				Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
				return new CommandExecuteResult(ExecuteResult: CommandExecuteResults.Wrong, Message: ex.Message);
			}
		}));
	}

	public async Task SetUser(User user)
	{
		Security security = await user.GetSecurity();
		_password = await security.GetPassword();
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.NewPassword,
			isPropertyValid: password => password?.Length >= 6,
			message: "Минимальная длина пароля - 6 символов."
		);

		this.ValidationRule(
			viewModelProperty: model => model.CurrentPassword,
			isPropertyValid: password => password?.Length >= 6,
			message: "Минимальная длина пароля - 6 символов."
		);

		IObservable<bool> passwordsObservable = this.WhenAnyValue(
			property1: model => model.NewPassword,
			property2: model => model.NewPasswordConfirmation,
			selector: (password, confirmation) => password == confirmation
		);

		this.ValidationRule(validationObservable: passwordsObservable, message: "Пароли не совпадают.");

		IObservable<bool> allPasswordsObservable = this.WhenAnyValue(
			property1: model => model.CurrentPassword,
			property2: model => model.NewPassword,
			property3: model => model.NewPasswordConfirmation,
			selector: (currentPassword, newPassword, newPasswordConfirmation)
				=> currentPassword != newPassword && currentPassword != newPasswordConfirmation
		);

		this.ValidationRule(validationObservable: allPasswordsObservable, message: "Новый пароль должен отличаться от текущего.");
	}
}