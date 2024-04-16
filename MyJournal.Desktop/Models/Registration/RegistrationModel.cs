using System.Reactive;
using Avalonia;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public class RegistrationModel : Drawable
{
	public RegistrationModel()
	{
		ToAuthorization = ReactiveCommand.Create(execute: MoveToAuthorization);
	}

	public ReactiveCommand<Unit, Unit> ToAuthorization { get; }

	public void MoveToAuthorization()
		=> MoveTo<AuthorizationVM>();
}