using System;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class AddAttachmentToTaskEventArgs(string pathToFile) : EventArgs
{
	public string PathToFile { get; } = pathToFile;
}