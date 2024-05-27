using System;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class RemoveSubjectOnTimetableEventArgs(SubjectOnTimetable subjectToRemove, Core.SubEntities.DayOfWeek dayOfWeek, int classId) : EventArgs
{
	public int ClassId { get; } = classId;
	public Core.SubEntities.DayOfWeek DayOfWeek { get; } = dayOfWeek;
	public SubjectOnTimetable SubjectToRemove { get; } = subjectToRemove;
}