namespace MyJournal.Core.SubEntities;

public sealed class EstimationOnTimetable(string grade)
{
	public string Grade { get; set; } = grade;
}