using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public class SecondStepOfRegistrationVM(SecondStepOfRegistrationModel model) : Renderer(model: model)
{
	public ReactiveCommand<Unit, Unit> ToBack => model.ToBack;
}