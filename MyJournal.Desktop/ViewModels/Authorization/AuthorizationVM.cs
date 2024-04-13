using System.Reactive;
using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Authorization;

public sealed class AuthorizationVM(AuthorizationModel model) : BaseVM(model: model)
{
	public WelcomeModel Presenter
	{
		get => model.Presenter;
		set => model.Presenter = value;
	}

	public ReactiveCommand<Unit, BaseVM> ToRegistration => model.ToRegistration;
}