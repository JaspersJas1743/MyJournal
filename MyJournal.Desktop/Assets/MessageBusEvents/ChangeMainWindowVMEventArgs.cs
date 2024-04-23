using System;
using Avalonia;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeMainWindowVMEventArgs(
	Type newVMType,
	AnimationType animationType
) : EventArgs
{
	public BaseVM NewVM { get; } = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
	public AnimationType AnimationType { get; } = animationType;
}