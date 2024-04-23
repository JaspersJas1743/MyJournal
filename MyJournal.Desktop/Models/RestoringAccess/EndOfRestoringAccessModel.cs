using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models.RestoringAccess;

public class EndOfRestoringAccessModel : ModelBase
{
	public EndOfRestoringAccessModel()
	{
		ToAuthorization = ReactiveCommand.CreateFromTask(execute: MoveToNextStep);
	}

	public ReactiveCommand<Unit, Unit> ToAuthorization { get; }

	public async Task MoveToNextStep()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: typeof(AuthorizationVM),
			animationType: AnimationType.DirectionToRight
		));
	}
}