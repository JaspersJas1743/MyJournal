using System;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeNumberOfSubjectOnTimetableEventArgs(int classId, int dayOfWeekId, SubjectOnTimetable subject) : EventArgs
{
	public SubjectOnTimetable Subject { get; } = subject;
	public int ClassId { get; } = classId;
	public int DayOfWeekId { get; } = dayOfWeekId;
}