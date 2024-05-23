using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncImageLoader;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using GnomeStack.Secrets.Linux;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Registration;
using MyJournal.Core.RestoringAccess;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ChatCreationService;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using MyJournal.Desktop.Assets.Utilities.ConfirmationService;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.Assets.Utilities.FileService;
using MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;
using MyJournal.Desktop.Assets.Utilities.MessagesService;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using MyJournal.Desktop.Assets.Utilities.ThemeConfigurationService;
using MyJournal.Desktop.Models;
using MyJournal.Desktop.Models.Authorization;
using MyJournal.Desktop.Models.ConfirmationCode;
using MyJournal.Desktop.Models.Marks;
using MyJournal.Desktop.Models.Profile;
using MyJournal.Desktop.Models.Registration;
using MyJournal.Desktop.Models.RestoringAccess;
using MyJournal.Desktop.Models.Tasks;
using MyJournal.Desktop.Models.Timetable;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Authorization;
using MyJournal.Desktop.ViewModels.ConfirmationCode;
using MyJournal.Desktop.ViewModels.Marks;
using MyJournal.Desktop.ViewModels.Profile;
using MyJournal.Desktop.ViewModels.Registration;
using MyJournal.Desktop.ViewModels.RestoringAccess;
using MyJournal.Desktop.ViewModels.Tasks;
using MyJournal.Desktop.ViewModels.Timetable;
using MyJournal.Desktop.Views;
using MyJournal.Desktop.Views.Authorization;
using MyJournal.Desktop.Views.ChatCreation;
using MyJournal.Desktop.Views.ConfirmationCode;
using MyJournal.Desktop.Views.Marks;
using MyJournal.Desktop.Views.Profile;
using MyJournal.Desktop.Views.Registration;
using MyJournal.Desktop.Views.RestoringAccess;
using MyJournal.Desktop.Views.Tasks;
using MyJournal.Desktop.Views.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop;

public partial class App : Application
{
	private readonly ServiceProvider _services;

	public App()
	{
#if RELEASE
		Dispatcher.UIThread.UnhandledException += OnUnhandledExceptionOnUIThread;
		RxApp.DefaultExceptionHandler = Observer.Create<Exception>(
			onNext: async exception => await HandleException(ex: exception),
			onError: async exception => await HandleException(ex: exception)
		);
#endif

		IServiceCollection services = new ServiceCollection()
		#region Services
			#region Main window
			.AddSingleton<MainWindowView>()
			.AddSingleton<MainWindowVM>()
			.AddSingleton<MainWindowModel>()
			#endregion
			#region Confirmation code window
			.AddTransient<FirstStepOfConfirmationView>()
			.AddTransient<SuccessConfirmationModel>()
			.AddTransient<SuccessConfirmationVM>()
			.AddTransient<SuccessConfirmationView>()
			#endregion
			#region Chat creation window
			.AddTransient<SingleChatCreationView>()
			.AddTransient<MultiChatCreationView>()
			#endregion
			#region Welcome
			.AddSingleton<WelcomeView>()
			.AddSingleton<WelcomeVM>()
			.AddSingleton<WelcomeModel>()
			#endregion
			#region Utilities
			.AddApiClient(timeout: TimeSpan.FromMinutes(value: 5))
			.AddGoogleAuthenticator()
			.AddFileService()
			.AddFileStorageService()
			.AddKeyedAuthorizationWithCredentialsService(key: nameof(AuthorizationWithCredentialsService))
			.AddKeyedAuthorizationWithTokenService(key: nameof(AuthorizationWithTokenService))
			.AddConfigurationService()
			.AddMessageService()
			.AddKeyedRegistrationCodeVerificationService(key: nameof(RegistrationCodeVerificationService))
			.AddKeyedLoginVerificationService(key: nameof(LoginVerificationService))
			.AddUserRegistrationService()
			.AddKeyedRestoringAccessThroughEmailService(key: nameof(RestoringAccessThroughEmailService))
			.AddKeyedRestoringAccessThroughPhoneService(key: nameof(RestoringAccessThroughPhoneService))
			.AddConfirmationService()
			.AddMenuConfigurationService()
			.AddThemeConfigurationService()
			.AddChatCreationService()
			.AddNotificationService()
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
			#region Last step
			.AddSingleton<EndOfRestoringAccessView>()
			.AddSingleton<EndOfRestoringAccessVM>()
			.AddSingleton<EndOfRestoringAccessModel>()
			#endregion
			#endregion
			#region Main
			.AddSingleton<MainView>()
			.AddSingleton<MainVM>()
			.AddSingleton<MainModel>()
			#endregion
			#region Initial loadin
			.AddSingleton<InitialLoadingView>()
			.AddSingleton<InitialLoadingVM>()
			.AddSingleton<InitialLoadingModel>()
			#endregion
			#region Profile
			.AddSingleton<ProfileView>()
			.AddSingleton<ProfileVM>()
			.AddSingleton<ProfileModel>()
			#region Profile photo
			.AddSingleton<ProfilePhotoView>()
			.AddSingleton<ProfilePhotoVM>()
			.AddSingleton<ProfilePhotoModel>()
			#endregion
			#region Profile email
			.AddSingleton<ProfileEmailView>()
			.AddSingleton<ProfileEmailVM>()
			.AddSingleton<ProfileEmailModel>()
			#endregion
			#region Profile phone
			.AddSingleton<ProfilePhoneView>()
			.AddSingleton<ProfilePhoneVM>()
			.AddSingleton<ProfilePhoneModel>()
			#endregion
			#region Profile sessions
			.AddSingleton<ProfileSessionsView>()
			.AddSingleton<ProfileSessionsVM>()
			.AddSingleton<ProfileSessionsModel>()
			#endregion
			#region Profile change menu type
			.AddSingleton<ProfileChangeMenuItemTypeView>()
			.AddSingleton<ProfileChangeMenuItemTypeVM>()
			.AddSingleton<ProfileChangeMenuItemTypeModel>()
			#endregion
			#region Profile change theme
			.AddSingleton<ProfileChangeThemeView>()
			.AddSingleton<ProfileChangeThemeVM>()
			.AddSingleton<ProfileChangeThemeModel>()
			#endregion
			#region Profile file storage
			.AddSingleton<ProfileFileStorageView>()
			.AddSingleton<ProfileFileStorageVM>()
			.AddSingleton<ProfileFileStorageModel>()
			#endregion
			#region Profile security
			.AddSingleton<ProfileSecurityView>()
			.AddSingleton<ProfileSecurityVM>()
			.AddSingleton<ProfileSecurityModel>()
			#endregion
			#region Profile started page
			.AddSingleton<ProfileChangeStartedPageView>()
			.AddSingleton<ProfileChangeStartedPageVM>()
			.AddSingleton<ProfileChangeStartedPageModel>()
			#endregion
			#endregion
			#region Messages
			.AddSingleton<MessagesView>()
			.AddSingleton<MessagesVM>()
			.AddSingleton<MessagesModel>()
			#endregion
			#region Marks
			#region Created marks
			.AddSingleton<CreatedMarksView>()
			.AddSingleton<CreatedMarksVM>()
			.AddSingleton<CreatedMarksModel>()
			#endregion
			#region Received marks
			.AddSingleton<ReceivedMarksView>()
			.AddSingleton<ReceivedMarksVM>()
			.AddSingleton<ReceivedMarksModel>()
			#endregion
			#endregion
			#region Tasks
			#region Created tasks
			.AddSingleton<CreatedTasksView>()
			.AddSingleton<CreatedTasksVM>()
			.AddSingleton<CreatedTasksModel>()
			#endregion
			#region Received tasks
			.AddSingleton<ReceivedTasksView>()
			.AddSingleton<ReceivedTasksVM>()
			.AddSingleton<ReceivedTasksModel>()
			#endregion
			#endregion
			#region Timetable
			#region Creating timetable
			.AddSingleton<CreatingTimetableView>()
			.AddSingleton<CreatingTimetableVM>()
			.AddSingleton<CreatingTimetableModel>()
			#endregion
			#region Study timetable
			.AddSingleton<StudyTimetableView>()
			.AddSingleton<StudyTimetableVM>()
			.AddSingleton<StudyTimetableModel>()
			#endregion
			#region Work timetable
			.AddSingleton<WorkTimetableView>()
			.AddSingleton<WorkTimetableVM>()
			.AddSingleton<WorkTimetableModel>();
			#endregion
			#endregion
		PlatformDetector.RunIfCurrentPlatformIsWindows(action: () => services.AddWindowsCredentialStorageService());
		PlatformDetector.RunIfCurrentPlatformIsLinux(action: () => services.AddLinuxCredentialStorageService());
		PlatformDetector.RunIfCurrentPlatformIsMacOS(action: () => services.AddMacOsCredentialStorageService());
		#endregion

		_services = services.BuildServiceProvider();
	}

	private async Task HandleException(Exception ex)
	{
		INotificationService notificationService = GetService<INotificationService>();
		string message = ex.Message;
		if (ex is SocketException or TaskCanceledException or HttpRequestException)
			message = "Проверьте Ваше интернет-соединение и повторите попытку позже!";

		if (!await CheckConnection())
			message = "В данный момент удаленный сервер недоступен :(\nПопробуйте попытку позже.";

		string content = String.Join("\n\t", ex.GetType().GetProperties().Select(selector: p => $"{p.Name}: {p.GetValue(obj: ex)}"));
		DirectoryInfo directory = Directory.CreateTempSubdirectory(prefix: "MyJournal");
		await File.WriteAllTextAsync(path: $"{directory.FullName}/log_{DateTime.Now:dd_MM_yyyy_hh_mm_ss}.txt", contents: content);
		
		await notificationService.Show(title: "Непредвиденная ошибка", content: message, type: NotificationType.Error);
	}

	private async Task<bool> CheckConnection()
	{
		try
		{
			Ping ping = new Ping();
			PingReply result = await ping.SendPingAsync(hostNameOrAddress: "my-journal.ru");
			return result.Status == IPStatus.Success;
		}
		catch (Exception e)
		{
			return false;
		}
	}

	private async void OnUnhandledExceptionOnUIThread(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		await HandleException(ex: e.Exception);
		e.Handled = true;
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

	public override async void OnFrameworkInitializationCompleted()
	{
		ImageLoader.AsyncImageLoader = new DiskCachedWebImageLoader();

		SetTheme(configurationService: GetService<IConfigurationService>());
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = GetService<MainWindowView>();
			MainWindowVM mainWindowVM = GetService<MainWindowVM>();
			InitialLoadingVM initialLoadingVM = GetService<InitialLoadingVM>();
			mainWindowVM.Content = initialLoadingVM;
			desktop.MainWindow.DataContext = mainWindowVM;

			ICredentialStorageService credentialStorageService = GetService<ICredentialStorageService>();
			UserCredential credential;
			try
			{
				credential = credentialStorageService.Get();
			}
			catch (GException ex)
			{
				credential = UserCredential.Empty;
			}
			if (credential != UserCredential.Empty)
			{
				try
				{
					IAuthorizationService<User> authorizationService =
						GetKeyedService<IAuthorizationService<User>>(key: nameof(AuthorizationWithTokenService));
					MainVM mainVM = GetService<MainVM>();
					Authorized<User> authorizedUser = await authorizationService.SignIn(
						credentials: new UserTokenCredentials(token: credential.AccessToken)
					);
					await mainVM.SetAuthorizedUser(user: authorizedUser.Instance);
					mainWindowVM.SetUser(user: authorizedUser.Instance);
					mainWindowVM.Content = mainVM;
				}
				catch (UnauthorizedAccessException e)
				{
					credentialStorageService.Remove();
					MoveToAuthorizationPage(mainWindowVM: mainWindowVM);
				}
				catch (HttpRequestException e)
				{
					INotificationService notificationService = GetService<INotificationService>();
					await notificationService.Show(
						title: "Непредвиденная ошибка",
						content: "Проверьте Ваше интернет-соединение и повторите попытку позже!",
						type: NotificationType.Error
					);
				}
			} else
				MoveToAuthorizationPage(mainWindowVM: mainWindowVM);

			initialLoadingVM.StopTimer();
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void MoveToAuthorizationPage(MainWindowVM mainWindowVM)
	{
		WelcomeVM welcomeVM = GetService<WelcomeVM>();
		mainWindowVM.Content = welcomeVM;
	}

	private void SetTheme(IConfigurationService configurationService)
	{
		ThemeVariant currentTheme = (typeof(ThemeVariant).GetProperty(
			name: configurationService.Get(key: ConfigurationKeys.Theme) ?? nameof(ThemeVariant.Default),
			bindingAttr: BindingFlags.Public | BindingFlags.Static
		)!.GetValue(obj: null) as ThemeVariant)!;
		IThemeConfigurationService.CurrentTheme = currentTheme == ThemeVariant.Default ? ActualThemeVariant : currentTheme;
		RequestedThemeVariant = IThemeConfigurationService.CurrentTheme;
	}
}