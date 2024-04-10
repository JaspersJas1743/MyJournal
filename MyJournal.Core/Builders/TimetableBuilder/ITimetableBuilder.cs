namespace MyJournal.Core.Builders.TimetableBuilder;

public interface ITimetableBuilder : IEnumerable<KeyValuePair<int, BaseTimetableForDayBuilder>>
{
	BaseTimetableForDayBuilder ForDay(int dayOfWeekId);
	Task Save(CancellationToken cancellationToken = default(CancellationToken));
}