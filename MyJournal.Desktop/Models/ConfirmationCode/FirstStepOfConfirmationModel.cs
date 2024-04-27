using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Threading;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ConfirmationService;
using MyJournal.Desktop.ViewModels.ConfirmationCode;
using ReactiveUI;

namespace MyJournal.Desktop.Models.ConfirmationCode;

public class FirstStepOfConfirmationModel : ModelWithErrorMessage
{
	private string _code = String.Empty;
	private string _text = String.Empty;

	public FirstStepOfConfirmationModel()
	{
		CompletedCode = ReactiveCommand.Create(execute: OnCompletedCode);
	}

	private void OnCompletedCode()
	{
		Command?.Execute(parameter: EntryCode).Subscribe(onNext: result =>
		{
			switch (result.ExecuteResult)
			{
				case CommandExecuteResults.Confirmed:
					SuccessConfirmationVM newVM = (Application.Current as App)!.GetService<SuccessConfirmationVM>();
					newVM.Text = result.Message;

					MessageBus.Current.SendMessage(message: new ChangeConfirmationCodeVMContentEventArgs(
						newVM: newVM, animationType: AnimationType.CrossFade
					));
					break;
				case CommandExecuteResults.Wrong:
					Dispatcher.UIThread.Invoke(callback: () => IConfirmationService.Instance?.Close(dialogResult: false));
					break;
				case CommandExecuteResults.Unconfirmed:
					Error = result.Message;
					Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
					break;
			}
		});
	}

	public ReactiveCommand<Unit, Unit> CompletedCode { get; }

	public ReactiveCommand<string, CommandExecuteResult>? Command { get; set; }

	public string EntryCode
	{
		get => _code;
		set => this.RaiseAndSetIfChanged(backingField: ref _code, newValue: value);
	}

	public string Text
	{
		get => _text;
		set => this.RaiseAndSetIfChanged(backingField: ref _text, newValue: value);
	}

	protected override void SetValidationRule() { }
}