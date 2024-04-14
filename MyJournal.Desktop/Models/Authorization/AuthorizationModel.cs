using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.ViewModels.Registration;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Authorization;

public class AuthorizationModel : Drawable
{
	private readonly IAuthorizationService<User> _authorizationService;

	private string _login = String.Empty;
	private string _password = String.Empty;
	private string _error = String.Empty;
	private bool _haveError = false;
	private bool _saveCredential = true;

	public AuthorizationModel(
		[FromKeyedServices(key: nameof(AuthorizationWithCredentialsService))] IAuthorizationService<User> authorizationService
	)
	{
		_authorizationService = authorizationService;

		this.WhenValueChanged(propertyAccessor: model => model.Error)
			.Select(selector: error => !String.IsNullOrEmpty(value: error))
			.Subscribe(onNext: hasError => HaveError = hasError);

		ToRegistration = ReactiveCommand.Create(execute: MoveToRegistration);
		ToRestoringAccess = ReactiveCommand.Create(execute: MoveToRestoringAccess);
		LogIn = ReactiveCommand.CreateFromTask(execute: LogInWithCredentials);
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
		set => this.RaiseAndSetIfChanged(ref _haveError, value);
	}

	public ReactiveCommand<Unit, Unit> ToRegistration { get; }
	public ReactiveCommand<Unit, Unit> ToRestoringAccess { get; }
	public ReactiveCommand<Unit, Unit> LogIn { get; }

	private void MoveToRegistration()
		=> MoveTo<FirstStepOfRegistrationVM>();

	private void MoveToRestoringAccess()
		=> MoveTo<RestoringAccessThroughEmailVM>();

	private async Task LogInWithCredentials(CancellationToken cancellationToken)
	{
		if (SaveCredential)
		{
		}
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
		}
		catch (ApiException e)
		{
			Error = e.Message;
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => Error = String.Empty);
		}
	}
}