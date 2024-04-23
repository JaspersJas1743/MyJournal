using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.Authorization;
using MyJournal.Core.Registration;
using MyJournal.Core.RestoringAccess;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.Assets.Utilities.MessagesService;
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
	private readonly ServiceProvider _services;

	public App()
	{
		IServiceCollection services = new ServiceCollection()
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
			.AddKeyedAuthorizationWithCredentialsService(key: nameof(AuthorizationWithCredentialsService))
			.AddKeyedAuthorizationWithTokenService(key: nameof(AuthorizationWithTokenService))
			.AddConfigurationService()
			.AddMessageService()
			.AddKeyedRegistrationCodeVerificationService(key: nameof(RegistrationCodeVerificationService))
			.AddKeyedLoginVerificationService(key: nameof(LoginVerificationService))
			.AddUserRegistrationService()
			.AddKeyedRestoringAccessThroughEmailService(key: nameof(RestoringAccessThroughEmailService))
			.AddKeyedRestoringAccessThroughPhoneService(key: nameof(RestoringAccessThroughPhoneService))
			#endregion
			#region Authorization
			.AddSingleton<AuthorizationView>()
			.AddSingleton<AuthorizationVM>()
			.AddSingleton<AuthorizationModel>()
			#endregion
			#region Registration
			#region First step
			.AddSingleton<FirstStepOfRegistrationView>()
			.AddSingleton<FirstStepOfRegistrationVM>()
			.AddSingleton<FirstStepOfRegistrationModel>()
			#endregion
			#region Second step
			.AddSingleton<SecondStepOfRegistrationView>()
			.AddSingleton<SecondStepOfRegistrationVM>()
			.AddSingleton<SecondStepOfRegistrationModel>()
			#endregion
			#region Third step
			.AddSingleton<ThirdStepOfRegistrationView>()
			.AddSingleton<ThirdStepOfRegistrationVM>()
			.AddSingleton<ThirdStepOfRegistrationModel>()
			#endregion
			#region Fourth step
			.AddSingleton<FourthStepOfRegistrationView>()
			.AddSingleton<FourthStepOfRegistrationVM>()
			.AddSingleton<FourthStepOfRegistrationModel>()
			#endregion
			#region Fifth step
			.AddSingleton<FifthStepOfRegistrationView>()
			.AddSingleton<FifthStepOfRegistrationVM>()
			.AddSingleton<FifthStepOfRegistrationModel>()
			#endregion
			#region Sixth step
			.AddSingleton<SixthStepOfRegistrationView>()
			.AddSingleton<SixthStepOfRegistrationVM>()
			.AddSingleton<SixthStepOfRegistrationModel>()
			#endregion
			#region Seventh step via phone
			.AddSingleton<SeventhStepOfRegistrationViaPhoneView>()
			.AddSingleton<SeventhStepOfRegistrationViaPhoneVM>()
			.AddSingleton<SeventhStepOfRegistrationViaPhoneModel>()
			#endregion
			#region Seventh step via email
			.AddSingleton<SeventhStepOfRegistrationViaEmailView>()
			.AddSingleton<SeventhStepOfRegistrationViaEmailVM>()
			.AddSingleton<SeventhStepOfRegistrationViaEmailModel>()
			#endregion
			#region End of registration
			.AddSingleton<EndOfRegistrationView>()
			.AddSingleton<EndOfRegistrationVM>()
			.AddSingleton<EndOfRegistrationModel>()
			#endregion
			#endregion
			#region Restoring Access
			#region Through email
			.AddSingleton<RestoringAccessThroughEmailView>()
			.AddSingleton<RestoringAccessThroughEmailVM>()
			.AddSingleton<RestoringAccessThroughEmailModel>()
			#endregion
			#region Through phone
			.AddSingleton<RestoringAccessThroughPhoneView>()
			.AddSingleton<RestoringAccessThroughPhoneVM>()
			.AddSingleton<RestoringAccessThroughPhoneModel>()
			#endregion
			#region Confirmation code
			.AddSingleton<ConfirmationOfRestoringAccessView>()
			.AddSingleton<ConfirmationOfRestoringAccessVM>()
			.AddSingleton<ConfirmationOfRestoringAccessModel>()
			#endregion
			#region Changing password
			.AddSingleton<ChangingPasswordWhenRestoringAccessView>()
			.AddSingleton<ChangingPasswordWhenRestoringAccessVM>()
			.AddSingleton<ChangingPasswordWhenRestoringAccessModel>()
			#endregion
			#endregion
			#region Main
			.AddSingleton<MainView>()
			.AddSingleton<MainVM>()
			.AddSingleton<MainModel>();
			#endregion

		PlatformDetector.RunIfCurrentPlatformIsWindows(action: () => services.AddWindowsCredentialStorageService());
		PlatformDetector.RunIfCurrentPlatformIsLinux(action: () => services.AddLinuxCredentialStorageService());
		PlatformDetector.RunIfCurrentPlatformIsMacOS(action: () => services.AddMacOsCredentialStorageService());

		_services = services.BuildServiceProvider();
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
			MainWindowVM mainWindowVM = GetService<MainWindowVM>();
			// ICredentialStorageService credentialStorageService = GetService<ICredentialStorageService>();
			// UserCredential credential = credentialStorageService.Get();
			// if (credential != UserCredential.Empty)
			// 	mainWindowVM.MainVM = GetService<MainVM>();

			desktop.MainWindow.DataContext = mainWindowVM;
		}

		base.OnFrameworkInitializationCompleted();
	}
}