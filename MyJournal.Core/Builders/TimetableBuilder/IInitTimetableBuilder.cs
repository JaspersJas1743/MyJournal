using MyJournal.Core.SubEntities;

namespace MyJournal.Core.Builders.TimetableBuilder;

public interface IInitTimetableBuilder
{
	ITimetableBuilder ForClass(int classId);
	ITimetableBuilder ForClass(int classId, IEnumerable<TimetableForClass> currentTimetable);
}