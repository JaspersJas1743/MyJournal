using System.Reactive;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using MyJournal.Desktop.Models.RestoringAccess;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.RestoringAccess;

public sealed class ChangingPasswordWhenRestoringAccessVM(ChangingPasswordWhenRestoringAccessModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;

	public IRestoringAccessService<User> RestoringAccessService
	{
		get => model.RestoringAccessService;
		set => model.RestoringAccessService = value;
	}

	public string NewPassword
	{
		get => model.NewPassword;
		set => model.NewPassword = value;
	}

	public string ConfirmationOfNewPassword
	{
		get => model.ConfirmationOfNewPassword;
		set => model.ConfirmationOfNewPassword = value;
	}
}