using System.Reactive;
using MyJournal.Desktop.Models.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Authorization;

public sealed class AuthorizationVM(AuthorizationModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToRegistration => model.ToRegistration;
	public ReactiveCommand<Unit, Unit> ToRestoringAccess => model.ToRestoringAccess;
	public ReactiveCommand<Unit, Unit> SignIn => model.SignIn;

	public string Login
	{
		get => model.Login;
		set => model.Login = value;
	}

	public string Password
	{
		get => model.Password;
		set => model.Password = value;
	}

	public string Error
	{
		get => model.Error;
		set => model.Error = value;
	}

	public bool HaveError => model.HaveError;

	public bool SaveCredential
	{
		get => model.SaveCredential;
		set => model.SaveCredential = value;
	}
}