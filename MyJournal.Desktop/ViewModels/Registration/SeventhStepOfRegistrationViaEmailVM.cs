using System.Reactive;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class SeventhStepOfRegistrationViaEmailVM(SeventhStepOfRegistrationViaEmailModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;

	public string Email
	{
		get => model.Email;
		set => model.Email = value;
	}
}