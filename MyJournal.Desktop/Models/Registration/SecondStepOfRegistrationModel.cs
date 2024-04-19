using System.Reactive;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public class SecondStepOfRegistrationModel : ModelBase
{
	public SecondStepOfRegistrationModel()
	{
		ToBack = ReactiveCommand.Create(execute: MoveToBack);
	}

	public ReactiveCommand<Unit, Unit> ToBack { get; }

	public void MoveToBack()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(FirstStepOfRegistrationVM),
			directionOfTransitionAnimation: PageTransition.Direction.Right
		));
	}
}