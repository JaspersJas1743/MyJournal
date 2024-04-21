using System.Reactive;
using Avalonia.Controls;
using MyJournal.Desktop.Models.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Registration;

public sealed class FourthStepOfRegistrationVM(FourthStepOfRegistrationModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ToNextStep => model.ToNextStep;
	public ReactiveCommand<Unit, Unit> ShowCode => model.ShowCode;
	public ReactiveCommand<Button, Unit> CopyToClipboard => model.CopyToClipboard;

	public string QRCode
	{
		get => model.QRCode;
		set => model.QRCode = value;
	}

	public string Code
	{
		get => model.Code;
		set => model.Code = value;
	}

	public bool CodeIsDisplayed => model.CodeIsDisplayed;
}