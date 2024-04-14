using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;
using MyJournal.Desktop.Models;
using MyJournal.Desktop.Models.Authorization;
using MyJournal.Desktop.Models.Registration;
using MyJournal.Desktop.Models.RestoringAccess;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Authorization;
using MyJournal.Desktop.ViewModels.Registration;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using MyJournal.Desktop.Views;
using MyJournal.Desktop.Views.Authorization;
using MyJournal.Desktop.Views.Registration;
using MyJournal.Desktop.Views.RestoringAccess;

namespace MyJournal.Desktop;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App()
	{
		_services = new ServiceCollection()
			#region MainWindow
			.AddSingleton<MainWindowView>()
			.AddSingleton<MainWindowVM>()
			.AddSingleton<MainWindowModel>()
			#endregion
			#region Welcome
			.AddSingleton<WelcomeView>()
			.AddSingleton<WelcomeVM>()
			.AddSingleton<WelcomeModel>()
			#endregion
			#region Utilities
			.AddApiClient()
			.AddGoogleAuthenticator()
			.AddFileService()
			#endregion
			#region Authorization
			.AddSingleton<AuthorizationView>()
			.AddKeyedSingleton<IAuthorizationService<User>, AuthorizationWithCredentialsService>(serviceKey: nameof(AuthorizationWithCredentialsService))
			.AddKeyedSingleton<IAuthorizationService<User>, AuthorizationWithTokenService>(serviceKey: nameof(AuthorizationWithTokenService))
			.AddSingleton<AuthorizationVM>()
			.AddSingleton<AuthorizationModel>()
			#endregion
			#region Registration
			.AddSingleton<FirstStepOfRegistrationView>()
			.AddSingleton<FirstStepOfRegistrationVM>()
			.AddSingleton<RegistrationModel>()
			#endregion
			#region Restoring Access
			.AddSingleton<RestoringAccessThroughEmailView>()
			.AddSingleton<RestoringAccessThroughEmailVM>()
			.AddSingleton<RestoringAccessThroughEmailModel>()
			#endregion
			.BuildServiceProvider();
	}

	public T GetService<T>()
	{
		return _services.GetService<T>() ??
			throw new ArgumentException(message: $"Сервис типа {typeof(T)} не найден.", paramName: nameof(T));
	}

	public T GetKeyedService<T>(string key)
	{
		return _services.GetKeyedService<T>(serviceKey: key) ??
			throw new ArgumentException(message: $"Сервис со значением {key} типа {typeof(T)} не найден.", paramName: nameof(key));
	}

	public object GetService(Type serviceType)
	{
		return _services.GetService(serviceType: serviceType) ??
			throw new ArgumentException(message: $"Сервис типа {serviceType} не найден.", paramName: nameof(serviceType));
	}

	public override void Initialize()
		=> AvaloniaXamlLoader.Load(obj: this);

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = GetService<MainWindowView>();
			desktop.MainWindow!.DataContext = GetService<MainWindowVM>();
		}

		base.OnFrameworkInitializationCompleted();
	}
}