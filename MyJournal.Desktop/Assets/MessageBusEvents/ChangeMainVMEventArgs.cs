using System;
using Avalonia;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeMainVMEventArgs(Type newVMType) : EventArgs
{
	public BaseVM NewVM { get; } = ((Application.Current as App)!.GetService(serviceType: newVMType) as BaseVM)!;
}