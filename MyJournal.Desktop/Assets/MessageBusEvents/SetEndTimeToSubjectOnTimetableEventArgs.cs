using System;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class SetEndTimeToSubjectOnTimetableEventArgs(
	TimeSpan endTime,
	SubjectOnTimetable changedSubject,
	Core.SubEntities.DayOfWeek dayOfWeek,
	int classId
) : EventArgs
{
	public TimeSpan End { get; } = endTime;
	public SubjectOnTimetable Subject { get; } = changedSubject;
	public Core.SubEntities.DayOfWeek DayOfWeek { get; } = dayOfWeek;
	public int ClassId { get; } = classId;
}