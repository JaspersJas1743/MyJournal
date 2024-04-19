using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class SecondStepOfRegistrationVM(SecondStepOfRegistrationModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToBack => model.ToBack;
}