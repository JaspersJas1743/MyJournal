using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class SixthStepOfRegistrationVM(SixthStepOfRegistrationModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;
	public ReactiveCommand<Unit, Unit> SelectPhone => model.SelectPhone;
	public ReactiveCommand<Unit, Unit> SelectEmail => model.SelectEmail;
}