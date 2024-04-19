using System;
using Avalonia;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeWelcomeVMContentEventArgs(
	Type newVMType,
	PageTransition.Direction directionOfTransitionAnimation
) : EventArgs
{
	public BaseVM NewVM { get; } = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
	public PageTransition.Direction DirectionOfTransitionAnimation { get; } = directionOfTransitionAnimation;
}