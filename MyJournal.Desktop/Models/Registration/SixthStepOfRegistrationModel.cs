using System;
using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public sealed class SixthStepOfRegistrationModel : ModelBase
{
	private Type _selectedOption = typeof(SeventhStepOfRegistrationViaPhoneVM);

	public SixthStepOfRegistrationModel()
	{
		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep);
		SelectPhone = ReactiveCommand.CreateFromTask(execute: SelectPhoneOption);
		SelectEmail = ReactiveCommand.CreateFromTask(execute: SelectEmailOption);
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public ReactiveCommand<Unit, Unit> SelectPhone { get; }
	public ReactiveCommand<Unit, Unit> SelectEmail { get; }

	public async Task MoveToNextStep()
	{
		MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			newVMType: _selectedOption,
			animationType: AnimationType.DirectionToRight
		));
	}

	public async Task SelectPhoneOption()
		=> _selectedOption = typeof(SeventhStepOfRegistrationViaPhoneVM);

	public async Task SelectEmailOption()
		=> _selectedOption = typeof(SeventhStepOfRegistrationViaEmailVM);
}