using System.Reactive;
using Avalonia;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class AuthorizationModel : ModelBase
{
	public AuthorizationModel()
	{
		ToRegistration = ReactiveCommand.Create(execute: () =>
		{
			RegistrationVM registrationVM = (Application.Current as App)!.GetService<RegistrationVM>();
			registrationVM.Presenter = Presenter;
			return Presenter!.Content = registrationVM;
		});
	}

	public ReactiveCommand<Unit, BaseVM> ToRegistration { get; }
	public WelcomeModel Presenter { get; set; }
}