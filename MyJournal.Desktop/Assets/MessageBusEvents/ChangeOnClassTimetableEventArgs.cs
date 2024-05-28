using System;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeOnClassTimetableEventArgs(int classId) : EventArgs
{
	public int ClassId { get; } = classId;
}