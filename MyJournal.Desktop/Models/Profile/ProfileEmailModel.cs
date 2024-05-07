using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ConfirmationService;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileEmailModel : ModelWithErrorMessage
{
	private const string ConfirmationMessage = """
											   Для изменения адреса электронной почты необходимо
											   подтвердить, что Вы - владелец аккаунта. Для этого 
											   откройте приложение Google Authenticator и
											   введите код для MyJournal
											   """;

	private readonly IConfirmationService _confirmationService;

	private string? _enteredEmail = String.Empty;
	private Email? _email = null;
	private bool _emailIsVerified = false;

	public ProfileEmailModel(IConfirmationService confirmationService)
	{
		_confirmationService = confirmationService;

		this.WhenAnyValue(property1: model => model.EnteredEmail).WhereNotNull()
			.Subscribe(onNext: _ => EmailIsVerified = !String.IsNullOrWhiteSpace(value: _email?.Address) && EnteredEmail == _email?.Address);

		ChangeEmail = ReactiveCommand.CreateFromTask(execute: ChangeUserEmail, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ChangeEmail { get; }

	public string? EnteredEmail
	{
		get => _enteredEmail;
		set => this.RaiseAndSetIfChanged(backingField: ref _enteredEmail, newValue: value);
	}

	public bool EmailIsVerified
	{
		get => _emailIsVerified;
		set => this.RaiseAndSetIfChanged(backingField: ref _emailIsVerified, newValue: value);
	}

	public async Task SetUser(User user)
	{
		Security security = await user.GetSecurity();
		_email = await security.GetEmail();
		_email.Updated += OnEmailUpdated;
		EnteredEmail = _email.Address ?? String.Empty;
	}

	private async Task ChangeUserEmail()
	{
		await _confirmationService.Сonfirm(text: ConfirmationMessage, command: ReactiveCommand.CreateFromTask(execute: async (string code) =>
		{
			try
			{
				string message = await _email?.Change(confirmationCode: code, newEmail: EnteredEmail);
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

	private void OnEmailUpdated(UpdatedEmailEventArgs e)
	{
		EnteredEmail = e.Email;
		EmailIsVerified = !String.IsNullOrWhiteSpace(value: _email?.Address) && EnteredEmail == _email?.Address;
	}

	protected override void SetValidationRule()
	{
		const string expression = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$";
		this.ValidationRule(
			viewModelProperty: model => model.EnteredEmail,
			isPropertyValid: email => Regex.IsMatch(input: email, pattern: expression),
			message: "Неверный формат адреса электронной почты."
		);

		this.ValidationRule(
			viewModelProperty: model => model.EmailIsVerified,
			isPropertyValid: isVerified => !isVerified,
			message: "Введенная электронная почта подтверждена."
		);
	}
}