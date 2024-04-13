using System.Reactive;
using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public class RegistrationVM(RegistrationModel model) : BaseVM(model: model)
{
	public WelcomeModel Presenter
	{
		get => model.Presenter;
		set => model.Presenter = value;
	}

	public ReactiveCommand<Unit, BaseVM> ToAuthorization => model.ToAuthorization;
}