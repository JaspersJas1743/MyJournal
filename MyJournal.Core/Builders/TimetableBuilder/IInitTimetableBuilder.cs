namespace MyJournal.Core.Builders.TimetableBuilder;

public interface IInitTimetableBuilder
{
	ITimetableBuilder ForClass(int classId);
}