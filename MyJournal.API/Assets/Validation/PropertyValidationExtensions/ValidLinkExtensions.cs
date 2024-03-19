using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class ValidLinkExtensions
{
	private static bool IsValidImageUrl(string? url)
	{
		using HttpClient client = new HttpClient();
		if (!Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, result: out Uri? uri))
			return false;

		HttpResponseMessage response = client.GetAsync(requestUri: uri).GetAwaiter().GetResult();
		return response.IsSuccessStatusCode && response.Content.Headers.ContentType.MediaType.StartsWith("image/");
	}

	private static bool IsValidUrl(string? url)
	{
		using HttpClient client = new HttpClient();
		if (!Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, result: out Uri? uri))
			return false;

		HttpResponseMessage response = client.GetAsync(requestUri: uri).GetAwaiter().GetResult();
		return response.IsSuccessStatusCode;
	}

	public static IRuleBuilderOptions<T, string?> IsValidImageUrl<T>(
		this IRuleBuilderOptions<T, string?> ruleBuilder
	) => ruleBuilder.Must(predicate: IsValidImageUrl);

	public static IRuleBuilderOptions<T, string?> IsValidImageUrl<T>(
		this IRuleBuilderInitial<T, string?> ruleBuilder
	) => ruleBuilder.Must(predicate: IsValidImageUrl);

	public static IRuleBuilderOptions<T, string?> IsValidUrl<T>(
		this IRuleBuilderOptions<T, string?> ruleBuilder
	) => ruleBuilder.Must(predicate: IsValidUrl);

	public static IRuleBuilderOptions<T, string?> IsValidUrl<T>(
		this IRuleBuilderInitial<T, string?> ruleBuilder
	) => ruleBuilder.Must(predicate: IsValidUrl);
}