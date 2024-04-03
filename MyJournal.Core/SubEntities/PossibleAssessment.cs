namespace MyJournal.Core.SubEntities;

public sealed class PossibleAssessment(int id, string assessment) : ISubEntity
{
	public int Id { get; init; } = id;
	public string Assessment { get; init; } = assessment;
}