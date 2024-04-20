using System;
using Avalonia;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeWelcomeVMContentEventArgs : EventArgs
{
	public ChangeWelcomeVMContentEventArgs(Type newVMType, PageTransition.Direction directionOfTransitionAnimation)
	{
		NewVM = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
		DirectionOfTransitionAnimation = directionOfTransitionAnimation;
	}

	public ChangeWelcomeVMContentEventArgs(BaseVM newVM, PageTransition.Direction directionOfTransitionAnimation)
	{
		NewVM = newVM;
		DirectionOfTransitionAnimation = directionOfTransitionAnimation;
	}

	public BaseVM NewVM { get; }
	public PageTransition.Direction DirectionOfTransitionAnimation { get; }
}