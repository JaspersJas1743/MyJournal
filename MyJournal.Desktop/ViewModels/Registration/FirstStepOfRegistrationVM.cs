using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class FirstStepOfRegistrationVM(FirstStepOfRegistrationModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;
	public ReactiveCommand<Unit, Unit> ToAuthorization => model.ToAuthorization;

	public string EntryCode
	{
		get => model.EntryCode;
		set => model.EntryCode = value;
	}

	public bool HaveError
	{
		get => model.HaveError;
		set => model.HaveError = value;
	}
}