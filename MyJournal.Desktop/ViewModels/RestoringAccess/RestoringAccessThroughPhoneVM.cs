using System.Reactive;
using MyJournal.Desktop.Models.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.RestoringAccess;

public sealed class RestoringAccessThroughPhoneVM(RestoringAccessThroughPhoneModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;
	public ReactiveCommand<Unit, Unit> ToRestoringAccessThroughEmail => model.ToRestoringAccessThroughEmail;

	public string Phone
	{
		get => model.Phone;
		set => model.Phone = value;
	}
}