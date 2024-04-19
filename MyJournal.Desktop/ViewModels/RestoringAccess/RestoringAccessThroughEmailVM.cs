using System.Reactive;
using MyJournal.Desktop.Models.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.RestoringAccess;

public sealed class RestoringAccessThroughEmailVM(RestoringAccessThroughEmailModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToAuthorization => model.ToAuthorization;
}