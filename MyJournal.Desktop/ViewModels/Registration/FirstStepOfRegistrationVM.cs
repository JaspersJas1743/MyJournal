using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public class FirstStepOfRegistrationVM(RegistrationModel model) : Renderer(model: model)
{
	public ReactiveCommand<Unit, Unit> ToAuthorization => model.ToAuthorization;
}