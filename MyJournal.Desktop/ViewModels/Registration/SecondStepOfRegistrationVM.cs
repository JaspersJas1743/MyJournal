using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class SecondStepOfRegistrationVM(SecondStepOfRegistrationModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;

	public string Login
	{
		get => model.Login;
		set => model.Login = value;
	}

	public void SetRegistrationCode(string code)
		=> model.RegistrationCode = code;
}