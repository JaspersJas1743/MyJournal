using System.Reactive;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public class SecondStepOfRegistrationModel : Drawable
{
	public SecondStepOfRegistrationModel()
	{
		ToBack = ReactiveCommand.Create(execute: MoveToBack);
	}

	public ReactiveCommand<Unit, Unit> ToBack { get; }

	public void MoveToBack()
		=> MoveTo<FirstStepOfRegistrationVM>();
}