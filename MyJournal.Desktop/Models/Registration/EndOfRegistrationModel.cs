using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public sealed class EndOfRegistrationModel : ModelBase
{
	public EndOfRegistrationModel()
	{
		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public async Task MoveToNextStep()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(AuthorizationVM),
			animationType: AnimationType.DirectionToRight
		));
	}
}