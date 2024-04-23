using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class FifthStepOfRegistrationVM(FifthStepOfRegistrationModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;

	public int CountOfCell => model.CountOfCell;

	public string EntryCode
	{
		get => model.EntryCode;
		set => model.EntryCode = value;
	}
}