using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MyJournal.Core.Utilities.Api;

public sealed class ApiClient : IDisposable
{
	#region Fields
	private const string ServerAddress = "https://localhost:7267/api/";

	private readonly HttpClient _client = new HttpClient()
	{
		Timeout = TimeSpan.FromSeconds(value: 2),
	};
	
	private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
	{
		WriteIndented = true,
		PropertyNamingPolicy = null
	};
	#endregion

	#region Constructors
	public ApiClient()
	{
		_client.DefaultRequestHeaders.Clear();
		_client.DefaultRequestHeaders.Add(name: nameof(HttpRequestHeader.Accept), value: MediaTypeNames.Application.Json);
	}
	#endregion

	#region Properties
	public string ContentType { get; private set; }
	public string FileName { get; private set; }
	public int SessionId { get; set; }
	public int ClientId { get; set; }

	public string? Token
	{
		get => _client.DefaultRequestHeaders.Authorization?.ToString();
		set => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: value);
	}
	#endregion

	#region Methods
	public void ResetToken()
		=> _client.DefaultRequestHeaders.Authorization = null;

	#region GET
	private async Task<HttpResponseMessage> HelperForGetAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage responseMessage = await _client.GetAsync(requestUri: uri, cancellationToken: cancellationToken);
        await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);

		if (responseMessage.Content.Headers.ContentType?.MediaType != null)
			ContentType = responseMessage.Content.Headers.ContentType.MediaType;

		if (responseMessage.Content.Headers.ContentDisposition?.FileNameStar != null)
			FileName = responseMessage.Content.Headers.ContentDisposition.FileNameStar;

		return responseMessage;
    }

    public async Task<TOut?> GetAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperForGetAsync(uri: uri, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: _options, cancellationToken: cancellationToken);
    }

	public async Task<TOut?> GetAsync<TOut>(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetAsync<TOut>(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

    public async Task<TOut?> GetAsync<TOut>(string apiMethod, Dictionary<string, string> argQuery, CancellationToken cancellationToken = default(CancellationToken))
        => await GetAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);

	public async Task<TOut?> GetAsync<TOut, TIn>(string apiMethod, TIn argQuery, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);

    public async Task<byte[]> GetBytesAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperForGetAsync(uri: uri, cancellationToken: cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken: cancellationToken);
    }

	public async Task<byte[]> GetBytesAsync(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetBytesAsync(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

    public async Task<byte[]> GetBytesAsync(string apiMethod, Dictionary<string, string> argQuery, CancellationToken cancellationToken = default(CancellationToken))
        => await GetBytesAsync(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);

	public async Task<byte[]> GetBytesAsync<TIn>(string apiMethod, TIn argQuery, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetBytesAsync(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);
	#endregion GET

    #region POST
	public async Task<TOut?> PostAsync<TOut, TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage response = await HelperForPostAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);
		return await response.Content.ReadFromJsonAsync<TOut>(options: _options, cancellationToken: cancellationToken);
	}

	public async Task<TOut?> PostAsync<TOut, TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PostAsync<TOut, TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

	public async Task PostAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPostAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);

    public async Task PostAsync<TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PostAsync<TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

	public async Task<TOut?> PostAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPostAsync<TOut>(uri: uri, cancellationToken: cancellationToken);

	public async Task<TOut?> PostAsync<TOut>(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await PostAsync<TOut>(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public async Task PostAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPostAsync(uri: uri, cancellationToken: cancellationToken);

	public async Task PostAsync(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await PostAsync(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public async Task PostAsync(string apiMethod, Dictionary<string, string> arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PostAsync(uri: CreateUri(apiMethod, arg: arg), cancellationToken: cancellationToken);

	private async Task HelperForPostAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperAsync(func: _client.PostAsync, uri: uri, cancellationToken: cancellationToken);

	private async Task<TOut?> HelperForPostAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperAsync<TOut>(func: _client.PostAsync, uri: uri, cancellationToken: cancellationToken);

	private async Task<HttpResponseMessage> HelperForPostAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperAsync(func: _client.PostAsync, uri: uri, arg: arg, cancellationToken: cancellationToken);

    public async Task PostFileAsync(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
        => await PostFileAsync(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public async Task PostFileAsync(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
            => await HelperFileAsync(func: _client.PostAsync, uri: uri, path: path, cancellationToken: cancellationToken);

    public async Task<TOut?> PostFileAsync<TOut>(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
        => await PostFileAsync<TOut>(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public async Task<TOut?> PostFileAsync<TOut>(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperFileAsync(func: _client.PostAsync, uri: uri, path: path, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: _options, cancellationToken: cancellationToken);
    }
    #endregion POST

    #region PUT
    public async Task<TOut?> PutAsync<TOut, TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PutAsync<TOut, TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

    public async Task<TOut?> PutAsync<TOut, TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperForPutAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: _options, cancellationToken: cancellationToken);
    }

	public async Task PutAsync(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await PutAsync(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public async Task PutAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPutAsync(uri: uri, cancellationToken: cancellationToken);

    public async Task PutAsync<TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PutAsync<TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

    public async Task PutAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperForPutAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);

	private async Task HelperForPutAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperAsync(func: _client.PutAsync, uri: uri, cancellationToken: cancellationToken);

	private async Task<TOut?> HelperForPutAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperAsync<TOut>(func: _client.PutAsync, uri: uri, cancellationToken: cancellationToken);

    private async Task<HttpResponseMessage> HelperForPutAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperAsync<TIn>(func: _client.PutAsync, uri: uri, arg: arg, cancellationToken: cancellationToken);

    public async Task PutFileAsync(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
       => await PutFileAsync(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public async Task PutFileAsync(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperFileAsync(func: _client.PutAsync, uri: uri, path: path, cancellationToken: cancellationToken);

    public async Task<TOut?> PutFileAsync<TOut>(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
       => await PutFileAsync<TOut>(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public async Task<TOut?> PutFileAsync<TOut>(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperFileAsync(func: _client.PutAsync, uri: uri, path: path, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: _options, cancellationToken: cancellationToken);
    }
    #endregion PUT

    #region DELETE
	public async Task<TOut?> DeleteAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await HelperForDeleteAsync(uri: uri, cancellationToken: cancellationToken);
		return await responseMessage.Content.ReadFromJsonAsync<TOut>(cancellationToken: cancellationToken);
	}

	public async Task<TOut?> DeleteAsync<TOut, TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await HelperForDeleteAsync(uri: uri, cancellationToken: cancellationToken);
		return await responseMessage.Content.ReadFromJsonAsync<TOut>(cancellationToken: cancellationToken);
	}

	public async Task<TOut?> DeleteAsync<TOut>(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync<TOut>(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public async Task<TOut?> DeleteAsync<TOut>(string apiMethod, Dictionary<string, string> arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	public async Task<TOut?> DeleteAsync<TIn, TOut>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	public async Task DeleteAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForDeleteAsync(uri: uri, cancellationToken: cancellationToken);

	public async Task DeleteAsync(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public async Task DeleteAsync(string apiMethod, Dictionary<string, string> arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	public async Task DeleteAsync<TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

	public async Task DeleteAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForDeleteAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);

	private async Task<HttpResponseMessage> HelperForDeleteAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await _client.DeleteAsync(requestUri: uri, cancellationToken: cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);
		return responseMessage;
	}

	private async Task<HttpResponseMessage> HelperForDeleteAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpRequestMessage requestMessage = new HttpRequestMessage()
		{
			Content = JsonContent.Create(inputValue: arg, inputType: arg!.GetType()),
			Method = HttpMethod.Delete,
			RequestUri = uri
		};
		HttpResponseMessage responseMessage = await _client.SendAsync(request: requestMessage);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);
		return responseMessage;
	}
    #endregion DELETE

	#region Helpers
	private async Task<HttpResponseMessage> HelperFileAsync(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		using MultipartFormDataContent data = new MultipartFormDataContent();
		byte[] fileBytes = await File.ReadAllBytesAsync(path: path, cancellationToken: cancellationToken);
		ByteArrayContent content = new ByteArrayContent(content: fileBytes);
		data.Add(content: content, name: "file", fileName: Path.GetFileName(path: path));

		try
		{
			HttpResponseMessage responseMessage = await func(uri, data, cancellationToken);
			await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);
			return responseMessage;
		}
		catch (HttpRequestException e)
		{
			throw new ApiException(message: "Файл слишком большой. Максимальый размер - 30Мбайт.");
		}
	}

	private async Task HelperAsync(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await func(uri, new StringContent(content: String.Empty), cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);
	}

	private async Task<TOut?> HelperAsync<TOut>(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await func(uri, new StringContent(content: String.Empty), cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);
		return await responseMessage.Content.ReadFromJsonAsync<TOut>(options: _options, cancellationToken: cancellationToken);
	}

	private async Task<HttpResponseMessage> HelperAsync<TIn>(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await func(uri, JsonContent.Create(inputValue: arg, inputType: arg!.GetType()), cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: _options);
		return responseMessage;
	}

	private Uri CreateUri(string apiMethod)
		=> new Uri(uriString: ServerAddress + apiMethod);

	private Uri CreateUri(string apiMethod, Dictionary<string, string> arg)
	{
		StringBuilder uri = new StringBuilder(value: ServerAddress + apiMethod + '?');
		foreach (KeyValuePair<string, string> pair in arg)
			uri.Append(value: $"{pair.Key}={pair.Value}&");

		uri.Remove(startIndex: uri.Length - 1, length: 1);
		return new Uri(uriString: uri.ToString());
	}

	private Uri CreateUri<T>(string apiMethod, T arg)
	{
		StringBuilder uri = new StringBuilder(value: ServerAddress + apiMethod + '?');
		foreach (PropertyInfo pair in typeof(T).GetProperties())
			uri.Append(value: $"{pair.Name}={pair.GetValue(obj: arg)}&");

		uri.Remove(startIndex: uri.Length - 1, length: 1);
		return new Uri(uriString: uri.ToString());
	}
	#endregion

	#region IDisposable
	public void Dispose()
		=> _client.Dispose();
	#endregion
	#endregion
}

public static class ApiClientExtension
{
	public static void AddApiClient(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<ApiClient>();

	public static void AddKeyedApiClient(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<ApiClient>(serviceKey: key);
}