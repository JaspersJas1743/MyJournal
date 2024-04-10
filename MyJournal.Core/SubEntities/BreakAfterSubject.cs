namespace MyJournal.Core.SubEntities;

public sealed class BreakAfterSubject(int countMinutes)
{
	public int CountMinutes { get; set; } = countMinutes;
}