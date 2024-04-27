using System;
using Avalonia;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeConfirmationCodeVMContentEventArgs : EventArgs
{
	public ChangeConfirmationCodeVMContentEventArgs(Type newVMType, AnimationType animationType)
	{
		NewVM = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
		AnimationType = animationType;
	}

	public ChangeConfirmationCodeVMContentEventArgs(BaseVM newVM, AnimationType animationType)
	{
		NewVM = newVM;
		AnimationType = animationType;
	}

	public BaseVM NewVM { get; }
	public AnimationType AnimationType { get; }
}