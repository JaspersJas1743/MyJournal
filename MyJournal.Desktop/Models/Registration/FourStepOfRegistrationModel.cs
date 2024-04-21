using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using MyJournal.Core;
using MyJournal.Core.Registration;
using MyJournal.Desktop.Views.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Registration;

public sealed class FourStepOfRegistrationModel : ModelBase
{
	private readonly IRegistrationService<User> _registrationService;

	private string _qrCode = String.Empty;
	private string _code = String.Empty;
	private bool _codeIsDisplayed = false;

	public FourStepOfRegistrationModel(
		IRegistrationService<User> registrationService
	)
	{
		_registrationService = registrationService;

		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep);
		ShowCode = ReactiveCommand.Create(execute: ShowCodeExecute);
		CopyToClipboard = ReactiveCommand.CreateFromTask(execute: async (Button button) => await CopyCodeToClipboard(button));
	}

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }
	public ReactiveCommand<Unit, Unit> ShowCode { get; }
	public ReactiveCommand<Button, Unit> CopyToClipboard { get; }

	public string QRCode
	{
		get => _qrCode;
		set => this.RaiseAndSetIfChanged(backingField: ref _qrCode, newValue: value);
	}

	public string Code
	{
		get => _code;
		set => this.RaiseAndSetIfChanged(backingField: ref _code, newValue: value);
	}

	public bool CodeIsDisplayed
	{
		get => _codeIsDisplayed;
		set => this.RaiseAndSetIfChanged(backingField: ref _codeIsDisplayed, newValue: value);
	}

	public async Task MoveToNextStep()
	{

	}

	public void ShowCodeExecute()
		=> CodeIsDisplayed = true;

	public async Task CopyCodeToClipboard(Button currentButton)
	{
		IClipboard? clipboard = TopLevel.GetTopLevel(visual: (Application.Current as App)!.GetService<FourStepOfRegistrationView>())!.Clipboard;
		await clipboard?.SetTextAsync(text: Code)!;
		FlyoutBase.ShowAttachedFlyout(flyoutOwner: currentButton);
	}
}