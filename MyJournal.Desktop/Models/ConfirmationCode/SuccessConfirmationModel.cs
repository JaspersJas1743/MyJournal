using System;
using System.Reactive.Linq;
using Avalonia.Threading;
using MyJournal.Desktop.Assets.Utilities.ConfirmationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.ConfirmationCode;

public class SuccessConfirmationModel : ModelBase
{
	private string _text;

	public SuccessConfirmationModel()
	{
		if (IConfirmationService.Instance is null)
			return;

		Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(
			onNext: _ => Dispatcher.UIThread.Invoke(callback: () => IConfirmationService.Instance?.Close(dialogResult: true))
		);
	}

	public string Text
	{
		get => _text;
		set => this.RaiseAndSetIfChanged(backingField: ref _text, newValue: value);
	}
}