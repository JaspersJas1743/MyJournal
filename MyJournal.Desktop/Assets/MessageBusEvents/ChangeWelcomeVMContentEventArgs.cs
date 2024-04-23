using System;
using Avalonia;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public enum AnimationType
{
	CrossFade,
	DirectionToLeft,
	DirectionToRight
}

public sealed class ChangeWelcomeVMContentEventArgs : EventArgs
{
	public ChangeWelcomeVMContentEventArgs(Type newVMType, AnimationType animationType)
	{
		NewVM = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
		AnimationType = animationType;
	}

	public ChangeWelcomeVMContentEventArgs(BaseVM newVM, AnimationType animationType)
	{
		NewVM = newVM;
		AnimationType = animationType;
	}

	public BaseVM NewVM { get; }
	public AnimationType AnimationType { get; }
}