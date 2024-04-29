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

public sealed class ProfilePhoneModel : ModelWithErrorMessage
{
	private const string ConfirmationMessage = """
											   Для изменения номера мобильного телефона необходимо
											   подтвердить, что Вы - владелец аккаунта. Для этого 
											   откройте приложение Google Authenticator и
											   введите код для MyJournal
											   """;

	private readonly IConfirmationService _confirmationService;

	private string? _enteredPhone = String.Empty;
	private Phone? _phone = null;
	private bool _phoneIsVerified = false;

	public ProfilePhoneModel(IConfirmationService confirmationService)
	{
		_confirmationService = confirmationService;

		this.WhenAnyValue(property1: model => model.EnteredPhone)
			.Where(predicate: email => email is not null)
			.Subscribe(onNext: _ => PhoneIsVerified = !String.IsNullOrWhiteSpace(value: _phone?.Number) && EnteredPhone == _phone?.Number);

		ChangePhone = ReactiveCommand.CreateFromTask(execute: ChangeUserPhone, canExecute: ValidationContext.Valid);
	}

	public ReactiveCommand<Unit, Unit> ChangePhone { get; }

	public string? EnteredPhone
	{
		get => _enteredPhone;
		set => this.RaiseAndSetIfChanged(backingField: ref _enteredPhone, newValue: value);
	}

	public bool PhoneIsVerified
	{
		get => _phoneIsVerified;
		set => this.RaiseAndSetIfChanged(backingField: ref _phoneIsVerified, newValue: value);
	}

	public async Task SetUser(User user)
	{
		Security security = await user.GetSecurity();
		_phone = await security.GetPhone();
		_phone.Updated += OnPhoneUpdated;
		EnteredPhone = _phone.Number ?? String.Empty;
	}

	private async Task ChangeUserPhone()
	{
		await _confirmationService.Сonfirm(text: ConfirmationMessage, command: ReactiveCommand.CreateFromTask(execute: async (string code) =>
		{
			try
			{
				string message = await _phone?.Change(confirmationCode: code, newPhone: EnteredPhone);
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

	private void OnPhoneUpdated(UpdatedPhoneEventArgs e)
	{
		EnteredPhone = e.Phone;
		PhoneIsVerified = !String.IsNullOrWhiteSpace(value: _phone?.Number) && EnteredPhone == _phone?.Number;
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.EnteredPhone,
			isPropertyValid: phone => Regex.IsMatch(input: phone, pattern: @"\+7\(\d{3}\)\d{3}-\d{4}"),
			message: "Неверный формат номера мобильного телефона."
		);

		this.ValidationRule(
			viewModelProperty: model => model.PhoneIsVerified,
			isPropertyValid: isVerified => !isVerified,
			message: "Введенный номер мобильного телефона подтвержден."
		);
	}
}