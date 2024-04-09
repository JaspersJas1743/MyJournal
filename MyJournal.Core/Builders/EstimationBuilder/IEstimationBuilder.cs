namespace MyJournal.Core.Builders.EstimationBuilder;

public interface IEstimationBuilder
{
	IEstimationBuilder WithGrade(int gradeId);
	IEstimationBuilder WithCreationDate(DateTime creationDate);
	IEstimationBuilder WithComment(int commentId);
	Task Save(CancellationToken cancellationToken = default(CancellationToken));
}