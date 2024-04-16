using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.ViewModels.Registration;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using MyJournal.Desktop.Views;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Authorization;

public class AuthorizationModel : Drawable
{
	private readonly IAuthorizationService<User> _authorizationService;
	private readonly ICredentialStorageService _credentialStorageService;

	private string _login = String.Empty;
	private string _password = String.Empty;
	private string _error = String.Empty;
	private bool _haveError = false;
	private bool _saveCredential = true;

	public AuthorizationModel(
		[FromKeyedServices(key: nameof(AuthorizationWithCredentialsService))] IAuthorizationService<User> authorizationService,
		ICredentialStorageService credentialStorageService
	)
	{
		_authorizationService = authorizationService;
		_credentialStorageService = credentialStorageService;

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
		=> MoveTo<FirstStepOfRegistrationVM>();

	private void MoveToRestoringAccess()
		=> MoveTo<RestoringAccessThroughEmailVM>();

	private async Task SignInWithCredentials(CancellationToken cancellationToken)
	{
		try
		{
			User user = await _authorizationService.SignIn(
				credentials: new UserAuthorizationCredentials(
					login: Login,
					password: Password,
					client: UserAuthorizationCredentials.Clients.Windows
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
			ButtonResult dialogResult = await MessageBoxManager.GetMessageBoxStandard(@params: new MessageBoxStandardParams()
			{
				ContentTitle = "Установка зависимостей",
				ContentMessage = "Для сохранения учетных данных на Linux приложение MyJournal использует libsecret.\n" +
								 "Для корректной работы необходимо установить необходимые зависимости. Установить?",
				Icon = Icon.Question,
				HyperLinkParams = new HyperLinkParams()
				{
					Text = "Сайт libsecret.",
					Action = () => Process.Start(fileName: "x-www-browser", arguments: "https://wiki.gnome.org/Projects/Libsecret")
				},
				ButtonDefinitions = ButtonEnum.YesNo,
				EnterDefaultButton = ClickEnum.Yes,
				EscDefaultButton = ClickEnum.No,
				ShowInCenter = true,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			}).ShowWindowDialogAsync(owner: (Application.Current as App)!.GetService<MainWindowView>());
			if (dialogResult == ButtonResult.Yes)
				Process.Start(startInfo: new ProcessStartInfo(fileName: "bash", arguments: "sudo apt-get install libsecret-1-dev"));
			else
				await MessageBoxManager.GetMessageBoxStandard(title: String.Empty, text: "Учетные данные не будут сохранены.").ShowAsync();
		}

		_credentialStorageService.Set(credential: new UserCredential(Login: Login, Password: Password));
	}
}