using System.Reactive;
using MyJournal.Desktop.Models.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.RestoringAccess;

public class RestoringAccessThroughEmailVM(RestoringAccessThroughEmailModel model) : Renderer(model: model)
{
	public ReactiveCommand<Unit, Unit> ToAuthorization => model.ToAuthorization;
}