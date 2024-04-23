using System.Reactive;
using MyJournal.Desktop.Models.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.RestoringAccess;

public sealed class RestoringAccessThroughEmailVM(RestoringAccessThroughEmailModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;
	public ReactiveCommand<Unit, Unit> ToRestoringAccessThroughPhone => model.ToRestoringAccessThroughPhone;

	public string Email
	{
		get => model.Email;
		set => model.Email = value;
	}
}