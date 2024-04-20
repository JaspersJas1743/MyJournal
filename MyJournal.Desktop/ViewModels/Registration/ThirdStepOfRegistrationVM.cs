using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class ThirdStepOfRegistrationVM(ThirdStepOfRegistrationModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;

	public string Password
	{
		get => model.Password;
		set => model.Password = value;
	}

	public string ConfirmationPassword
	{
		get => model.ConfirmationPassword;
		set => model.ConfirmationPassword = value;
	}

	public void SetData(string login, string registrationCode)
	{
		model.Login = login;
		model.RegistrationCode = registrationCode;
	}
}