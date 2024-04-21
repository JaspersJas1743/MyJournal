using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class SeventhStepOfRegistrationViaPhoneVM(SeventhStepOfRegistrationViaPhoneModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;
}