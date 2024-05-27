using System;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class SetStartTimeToSubjectOnTimetableEventArgs(
	TimeSpan startTime,
	SubjectOnTimetable changedSubject,
	Core.SubEntities.DayOfWeek dayOfWeek,
	int classId
) : EventArgs
{
	public TimeSpan Start { get; } = startTime;
	public SubjectOnTimetable Subject { get; } = changedSubject;
	public Core.SubEntities.DayOfWeek DayOfWeek { get; } = dayOfWeek;
	public int ClassId { get; } = classId;
}