using System.Reactive;
using Avalonia;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class RegistrationModel : ModelBase
{
	public RegistrationModel()
	{
		ToAuthorization = ReactiveCommand.Create(execute: () =>
		{
			AuthorizationVM authorizationVM = (Application.Current as App)!.GetService<AuthorizationVM>();
			authorizationVM.Presenter = Presenter!;
			return Presenter!.Content = authorizationVM;
		});
	}

	public ReactiveCommand<Unit, BaseVM> ToAuthorization { get; }
	public WelcomeModel Presenter { get; set; }

}