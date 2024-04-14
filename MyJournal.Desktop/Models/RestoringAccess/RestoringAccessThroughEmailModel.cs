using System.Reactive;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models.RestoringAccess;

public class RestoringAccessThroughEmailModel : Drawable
{
	public RestoringAccessThroughEmailModel()
	{
		ToAuthorization = ReactiveCommand.Create(execute: MoveToAuthorization);
	}

	public ReactiveCommand<Unit, Unit> ToAuthorization { get; }

	public void MoveToAuthorization()
		=> MoveTo<AuthorizationVM>();
}