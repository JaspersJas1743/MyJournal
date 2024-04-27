using System.Reactive;
using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public sealed class ConfirmationCodeWindowVM(ConfirmationCodeWindowModel model) : VMWithError(model: model)
{
	public ReactiveCommand<Unit, Unit> Close => model.Close;
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