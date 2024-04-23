using System.Reactive;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using MyJournal.Desktop.Models.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.RestoringAccess;

public sealed class SecondStepOfRestoringAccessVM(SecondStepOfRestoringAccessModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;

	public IRestoringAccessService<User> RestoringAccessService
	{
		get => model.RestoringAccessService;
		set => model.RestoringAccessService = value;
	}

	public int CountOfCell => model.CountOfCell;

	public string EntryCode
	{
		get => model.EntryCode;
		set => model.EntryCode = value;
	}
}