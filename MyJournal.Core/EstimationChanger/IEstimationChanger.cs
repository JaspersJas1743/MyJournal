namespace MyJournal.Core.EstimationChanger;

public interface IEstimationChanger
{
	IEstimationChanger GradeTo(int newGradeId);
	IEstimationChanger DatetimeTo(DateTime newDateTime);
	IEstimationChanger CommentTo(int newCommentId);
	Task Save(CancellationToken cancellationToken = default(CancellationToken));
}