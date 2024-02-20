namespace MyJournal.API.Assets.Utilities;

[Serializable]
public sealed class HttpResponseException(int statusCode, string? message = default(String)) : Exception(message: message)
{
	public int StatusCode { get; init; } = statusCode;
}