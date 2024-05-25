using System;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeConfirmationCodeVMContentEventArgs(BaseVM newVM, AnimationType animationType) : EventArgs
{
	public BaseVM NewVM { get; } = newVM;
	public AnimationType AnimationType { get; } = animationType;
}