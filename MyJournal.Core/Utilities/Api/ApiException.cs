using System.Net;
using System.Text.Json;

namespace MyJournal.Core.Utilities.Api;

public record Error(string Message);

[Serializable]
public sealed class ApiException : Exception
{
	private static HttpStatusCode[] _handlers = new HttpStatusCode[]
	{
		HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized
	};

	public ApiException() { }

	public ApiException(string? message)
		: base(message: message) { }

	public ApiException(string? message, Exception innerException)
		: base(message: message, innerException: innerException) { }

	public static async Task ThrowIfErrorAsync(HttpResponseMessage message)
		=> await ThrowIfErrorAsync(message: message, options: new JsonSerializerOptions());

	public static async Task ThrowIfErrorAsync(HttpResponseMessage message, JsonSerializerOptions options)
	{
		if (!_handlers.Contains(value: message.StatusCode))
			return;

		if (message.StatusCode.Equals(obj: HttpStatusCode.Unauthorized))
			throw new UnauthorizedAccessException(message: "Некорректный авторизационный токен.");

		Error? error = await JsonSerializer.DeserializeAsync<Error>(
			utf8Json: await message.Content.ReadAsStreamAsync(),
			options: options
		);

		throw new ApiException(message: error?.Message);
	}
}