using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using MyJournal.Desktop.Assets.Utilities.ConfirmationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class ConfirmationCodeWindowModel : ModelWithErrorMessage
{
	private string _code = String.Empty;
	private string _text = String.Empty;

	public ConfirmationCodeWindowModel()
	{
		Close = ReactiveCommand.Create(execute: OnWindowClosed);
		CompletedCode = ReactiveCommand.Create(execute: OnCompletedCode);
	}

	private void OnCompletedCode()
	{
		Command?.Execute(parameter: EntryCode).Subscribe(onNext: result =>
		{
			if (String.IsNullOrEmpty(value: result))
				Dispatcher.UIThread.Invoke(callback: () => IConfirmationService.Instance?.Close(dialogResult: true));
			else
			{
				Error = result;
				Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
			}
		});
	}

	public ReactiveCommand<Unit, Unit> Close { get; }
	public ReactiveCommand<Unit, Unit> CompletedCode { get; }

	public ReactiveCommand<string, string>? Command { get; set; }

	private void OnWindowClosed()
		=> IConfirmationService.Instance?.Close(dialogResult: false);

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