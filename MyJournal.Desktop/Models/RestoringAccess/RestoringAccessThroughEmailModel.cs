using System.Reactive;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models.RestoringAccess;

public class RestoringAccessThroughEmailModel : ModelBase
{
	public RestoringAccessThroughEmailModel()
	{
		ToAuthorization = ReactiveCommand.Create(execute: MoveToAuthorization);
	}

	public ReactiveCommand<Unit, Unit> ToAuthorization { get; }

	public void MoveToAuthorization()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(AuthorizationVM),
			directionOfTransitionAnimation: PageTransition.Direction.Right
		));
	}
}