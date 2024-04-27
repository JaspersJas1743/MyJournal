using System.Reactive;
using MyJournal.Desktop.Models.ConfirmationCode;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.ConfirmationCode;

public sealed class FirstStepOfConfirmationVM(FirstStepOfConfirmationModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> CompletedCode => model.CompletedCode;

	public string EntryCode
	{
		get => model.EntryCode;
		set => model.EntryCode = value;
	}

	public string Text
	{
		get => model.Text;
		set => model.Text = value;
	}
}