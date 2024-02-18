namespace MyJournal.API.Assets.Utilities;

public class ApiError
{
	public string Message { get; set; } = null!;

	public static ApiError Create(string message)
	{
		return new ApiError
		{
			Message = message
		};
	}
}