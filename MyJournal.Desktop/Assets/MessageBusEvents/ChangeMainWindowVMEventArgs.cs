using System;
using Avalonia;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeMainWindowVMEventArgs : EventArgs
{
	public ChangeMainWindowVMEventArgs(Type newVMType, AnimationType animationType)
	{
		NewVM = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
		AnimationType = animationType;
	}

	public ChangeMainWindowVMEventArgs(BaseVM newVM, AnimationType animationType)
	{
		NewVM = newVM;
		AnimationType = animationType;
	}

	public BaseVM NewVM { get; }
	public AnimationType AnimationType { get; }
}