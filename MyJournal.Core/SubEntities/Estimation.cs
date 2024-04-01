namespace MyJournal.Core.SubEntities;

public enum GradeTypes
{
	Assessment,
	Truancy
}

public class Estimation
{
	protected Estimation(
		int id,
		string assessment,
		DateTime createdAt,
		string? comment,
		string? description,
		GradeTypes gradeType
	)
	{
		Id = id;
		Assessment = assessment;
		CreatedAt = createdAt;
		Comment = comment;
		Description = description;
		GradeType = gradeType;
	}

	public int Id { get; }
	public string Assessment { get; }
	public DateTime CreatedAt { get; }
	public string? Comment { get; }
	public string? Description { get; }
	public GradeTypes GradeType { get; }

	internal static async Task<Estimation> Create(
		int id,
		string assessment,
		DateTime createdAt,
		string? comment,
		string? description,
		GradeTypes gradeType
	)
	{
		return new Estimation(
			id: id,
			assessment: assessment,
			createdAt: createdAt,
			comment: comment,
			description: description,
			gradeType: gradeType
		);
	}
}