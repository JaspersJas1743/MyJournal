using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.Assets.Utilities.MessagesService;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Registration;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.Authorization;

public class AuthorizationModel : ModelWithErrorMessage
{
	private readonly IAuthorizationService<User> _authorizationService;
	private readonly ICredentialStorageService _credentialStorageService;
	private readonly IMessageService _messageService;

	private string _login = String.Empty;
	private string _password = String.Empty;
	private bool _saveCredential = true;

	public AuthorizationModel(
		[FromKeyedServices(key: nameof(AuthorizationWithCredentialsService))] IAuthorizationService<User> authorizationService,
		ICredentialStorageService credentialStorageService,
		IMessageService messageService
	)
	{
		_authorizationService = authorizationService;
		_credentialStorageService = credentialStorageService;
		_messageService = messageService;

		ToRegistration = ReactiveCommand.Create(execute: MoveToRegistration);
		ToRestoringAccess = ReactiveCommand.Create(execute: MoveToRestoringAccess);
		SignIn = ReactiveCommand.CreateFromTask(execute: SignInWithCredentials, canExecute: ValidationContext.Valid);
	}

	public string Login
	{
		get => _login;
		set => this.RaiseAndSetIfChanged(backingField: ref _login, newValue: value);
	}

	public string Password
	{
		get => _password;
		set => this.RaiseAndSetIfChanged(backingField: ref _password, newValue: value);
	}

	public bool SaveCredential
	{
		get => _saveCredential;
		set => this.RaiseAndSetIfChanged(backingField: ref _saveCredential, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> ToRegistration { get; }
	public ReactiveCommand<Unit, Unit> ToRestoringAccess { get; }
	public ReactiveCommand<Unit, Unit> SignIn { get; }

	private void MoveToRegistration()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(FirstStepOfRegistrationVM),
			animationType: AnimationType.DirectionToRight
		));
	}

	private void MoveToRestoringAccess()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(RestoringAccessThroughEmailVM),
			animationType: AnimationType.CrossFade
		));
	}

	private async Task SignInWithCredentials()
	{
		try
		{
			Authorized<User> authorizedUser = await _authorizationService.SignIn(
				credentials: new UserAuthorizationCredentials(
					login: Login,
					password: Password,
					client: Enum.Parse<UserAuthorizationCredentials.Clients>(value: PlatformDetector.CurrentOperatingSystem)
				)
			);

			if (SaveCredential)
				await SaveCorrectCredential(accessToken: authorizedUser.Token);

			MainVM mainVM = (Application.Current as App)!.GetService<MainVM>();
			await mainVM.SetAuthorizedUser(user: authorizedUser.Instance);

			MessageBus.Current.SendMessage(message: new ChangeMainWindowVMEventArgs(
				newVM: mainVM, animationType: AnimationType.DirectionToRight
			));
		}
		catch (ApiException e)
		{
			Error = e.Message;
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		}
	}

	private async Task SaveCorrectCredential(string accessToken)
	{
		if (_credentialStorageService.GetType().IsEquivalentTo(other: typeof(LinuxCredentialStorageService)))
		{
			ButtonResult dialogResult = await _messageService.ShowWindow(
				text: "Для сохранения учетных данных на Linux приложение MyJournal использует libsecret.\n" +
					  "Для корректной работы необходимо установить необходимые зависимости. Установить?",
				title: "Установка зависимостей",
				buttons: ButtonEnum.YesNo,
				image: Icon.Question
			);
			if (dialogResult == ButtonResult.Yes)
				Process.Start(startInfo: new ProcessStartInfo(fileName: "/bin/bash", arguments: "-c sudo apt-get install libsecret-1-dev"));
			else
			{
				_ = _messageService.ShowMessageWindow(text: "Учетные данные не будут сохранены.");
				return;
			}
		}

		_credentialStorageService.Set(credential: new UserCredential(Login: Login, AccessToken: accessToken));
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.Login,
			isPropertyValid: login => login?.Length >= 4,
			message: "Минимальная длина логина - 4 символа."
		);

		this.ValidationRule(
			viewModelProperty: model => model.Password,
			isPropertyValid: password => password?.Length >= 6,
			message: "Минимальная длина пароля - 6 символов."
		);
	}
}