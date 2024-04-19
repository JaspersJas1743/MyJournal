using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.Assets.Utilities.MessagesService;
using MyJournal.Desktop.ViewModels.Registration;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Authorization;

public class AuthorizationModel : ModelBase
{
	private readonly IAuthorizationService<User> _authorizationService;
	private readonly ICredentialStorageService _credentialStorageService;
	private readonly IMessageService _messageService;

	private string _login = String.Empty;
	private string _password = String.Empty;
	private string _error = String.Empty;
	private bool _haveError = false;
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

		this.WhenValueChanged(propertyAccessor: model => model.Error)
			.Select(selector: error => !String.IsNullOrEmpty(value: error))
			.Subscribe(onNext: hasError => HaveError = hasError);

		this.WhenValueChanged(propertyAccessor: model => model.HaveError)
			.Where(predicate: hasError => !hasError)
			.Subscribe(onNext: _ => Error = String.Empty);

		ToRegistration = ReactiveCommand.Create(execute: MoveToRegistration);
		ToRestoringAccess = ReactiveCommand.Create(execute: MoveToRestoringAccess);
		SignIn = ReactiveCommand.CreateFromTask(
			execute: SignInWithCredentials,
			canExecute: SignInWithCredentialsCanExecute()
		);
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

	public string Error
	{
		get => _error;
		set => this.RaiseAndSetIfChanged(backingField: ref _error, newValue: value);
	}

	public bool SaveCredential
	{
		get => _saveCredential;
		set => this.RaiseAndSetIfChanged(backingField: ref _saveCredential, newValue: value);
	}

	public bool HaveError
	{
		get => _haveError;
		private set => this.RaiseAndSetIfChanged(ref _haveError, value);
	}

	public ReactiveCommand<Unit, Unit> ToRegistration { get; }
	public ReactiveCommand<Unit, Unit> ToRestoringAccess { get; }
	public ReactiveCommand<Unit, Unit> SignIn { get; }

	private void MoveToRegistration()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(FirstStepOfRegistrationVM),
			directionOfTransitionAnimation: PageTransition.Direction.Left
		));
	}

	private void MoveToRestoringAccess()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(RestoringAccessThroughEmailVM),
			directionOfTransitionAnimation: PageTransition.Direction.Left
		));
	}

	private async Task SignInWithCredentials(CancellationToken cancellationToken)
	{
		try
		{
			Authorized<User> authorizedUser = await _authorizationService.SignIn(
				credentials: new UserAuthorizationCredentials(
					login: Login,
					password: Password,
					client: Enum.Parse<UserAuthorizationCredentials.Clients>(value: PlatformDetector.CurrentOperatingSystem)
				),
				cancellationToken: cancellationToken
			);

			if (SaveCredential)
				await SaveCorrectCredential();
		}
		catch (ApiException e)
		{
			Error = e.Message;
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		}
	}

	private IObservable<bool> SignInWithCredentialsCanExecute()
	{
		return this.WhenAnyValue(property1: model => model.Login, property2: model => model.Password)
			.Select(selector: tuple => tuple.Item1.Length >= 4 && tuple.Item2.Length >= 6);
	}

	private async Task SaveCorrectCredential()
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
				Process.Start(startInfo: new ProcessStartInfo(fileName: "bash", arguments: "sudo apt-get install libsecret-1-dev"));
			else
			{
				_ = _messageService.ShowMessageWindow(text: "Учетные данные не будут сохранены.");
				return;
			}
		}

		_credentialStorageService.Set(credential: new UserCredential(Login: Login, Password: Password));
	}
}