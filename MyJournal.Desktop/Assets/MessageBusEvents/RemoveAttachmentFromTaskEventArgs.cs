using System;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class RemoveAttachmentFromTaskEventArgs(string pathToFile) : EventArgs
{
	public string PathToFile { get; } = pathToFile;
}