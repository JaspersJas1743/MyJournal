using System;
using System.Reactive;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class AuthorizationModel : ModelBase
{
	private string _login = String.Empty;
	private string _password = String.Empty;
	private string _error = String.Empty;

	public AuthorizationModel(
		[FromKeyedServices(key: nameof(AuthorizationWithCredentialsService))] IAuthorizationService<User> authorizationService
	)
	{
		ToRegistration = ReactiveCommand.Create(execute: () =>
		{
			RegistrationVM registrationVM = (Application.Current as App)!.GetService<RegistrationVM>();
			registrationVM.Presenter = Presenter!;
			return Presenter!.Content = registrationVM;
		});
		LogIn = ReactiveCommand.CreateFromTask(execute: async ct =>
		{
			try
			{
				User user = await authorizationService.SignIn(credentials: new UserAuthorizationCredentials(
					login: Login,
					password: Password,
					client: UserAuthorizationCredentials.Clients.Windows
				), cancellationToken: ct);
			}
			catch (ApiException e)
			{
				Error = e.Message;
			}
		});
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

	public ReactiveCommand<Unit, BaseVM> ToRegistration { get; }
	public ReactiveCommand<Unit, Unit> LogIn { get; }
	public WelcomeModel Presenter { get; set; }
}