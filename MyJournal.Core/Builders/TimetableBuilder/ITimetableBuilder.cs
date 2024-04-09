namespace MyJournal.Core.Builders.TimetableBuilder;

public interface ITimetableBuilder
{
	BaseTimetableForDayBuilder ForDay(int dayOfWeekId);
	Task Save(CancellationToken cancellationToken = default(CancellationToken));
}