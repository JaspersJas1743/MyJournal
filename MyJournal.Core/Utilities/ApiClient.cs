using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MyJournal.Core.Utilities;

public static class ApiClient
{
	#region Fields
	private const string ServerAddress = "https://localhost:7267/api/";

	private static readonly HttpClient Client = new HttpClient()
	{
		Timeout = TimeSpan.FromSeconds(value: 2),
	};
	
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
	{
		WriteIndented = true,
		PropertyNamingPolicy = null
	};
	#endregion

	#region Constructors
	static ApiClient()
	{
		Client.DefaultRequestHeaders.Clear();
		Client.DefaultRequestHeaders.Add(name: "Accept", value: "application/json");
	}
	#endregion

	#region Properties
	public static string ContentType { get; set; }
	#endregion

	#region Methods
	#region GET
	private static async Task<HttpResponseMessage> HelperForGetAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage responseMessage = await Client.GetAsync(requestUri: uri, cancellationToken: cancellationToken);
        await ApiException.ThrowIfErrorAsync(message: responseMessage, options: Options);

		if (responseMessage.Content.Headers.ContentType?.MediaType != null)
			ContentType = responseMessage.Content.Headers.ContentType.MediaType;

		return responseMessage;
    }

    public static async Task<TOut?> GetAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperForGetAsync(uri: uri, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: Options, cancellationToken: cancellationToken);
    }

	public static async Task<TOut?> GetAsync<TOut>(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetAsync<TOut>(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

    public static async Task<TOut?> GetAsync<TOut>(string apiMethod, Dictionary<string, string> argQuery, CancellationToken cancellationToken = default(CancellationToken))
        => await GetAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);

	public static async Task<TOut?> GetAsync<TOut, TIn>(string apiMethod, TIn argQuery, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);

    public static async Task<byte[]> GetBytesAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperForGetAsync(uri: uri, cancellationToken: cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken: cancellationToken);
    }

    public static async Task<byte[]> GetBytesAsync(string apiMethod, Dictionary<string, string> argQuery, CancellationToken cancellationToken = default(CancellationToken))
        => await GetBytesAsync(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);

	public static async Task<byte[]> GetBytesAsync<TIn>(string apiMethod, TIn argQuery, CancellationToken cancellationToken = default(CancellationToken))
		=> await GetBytesAsync(uri: CreateUri(apiMethod: apiMethod, arg: argQuery), cancellationToken: cancellationToken);
	#endregion GET

    #region POST
	public static async Task<TOut?> PostAsync<TOut, TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage response = await HelperForPostAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);
		return await response.Content.ReadFromJsonAsync<TOut>(options: Options, cancellationToken: cancellationToken);
	}

	public static async Task<TOut?> PostAsync<TOut, TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PostAsync<TOut, TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

	public static async Task PostAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPostAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);

    public static async Task PostAsync<TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PostAsync<TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

	public static async Task<TOut?> PostAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPostAsync<TOut>(uri: uri, cancellationToken: cancellationToken);

	public static async Task<TOut?> PostAsync<TOut>(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await PostAsync<TOut>(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public static async Task PostAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForPostAsync(uri: uri, arg: String.Empty, cancellationToken: cancellationToken);

	public static async Task PostAsync(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await PostAsync(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public static async Task PostAsync(string apiMethod, Dictionary<string, string> arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PostAsync(uri: CreateUri(apiMethod, arg: arg), cancellationToken: cancellationToken);

	private static async Task<TOut?> HelperForPostAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperAsync<TOut>(func: Client.PostAsync, uri: uri, cancellationToken: cancellationToken);

	private static async Task<HttpResponseMessage> HelperForPostAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperAsync(func: Client.PostAsync, uri: uri, arg: arg, cancellationToken: cancellationToken);

    public static async Task PostFileAsync(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
        => await PostFileAsync(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public static async Task PostFileAsync(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
            => await HelperFileAsync(func: Client.PostAsync, uri: uri, path: path, cancellationToken: cancellationToken);

    public static async Task<TOut?> PostFileAsync<TOut>(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
        => await PostFileAsync<TOut>(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public static async Task<TOut?> PostFileAsync<TOut>(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperFileAsync(func: Client.PostAsync, uri: uri, path: path, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: Options, cancellationToken: cancellationToken);
    }
    #endregion POST

    #region PUT
    public static async Task<TOut?> PutAsync<TOut, TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PutAsync<TOut, TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

    public static async Task<TOut?> PutAsync<TOut, TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperForPutAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: Options, cancellationToken: cancellationToken);
    }

    public static async Task PutAsync<TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await PutAsync<TIn>(uri: CreateUri(apiMethod: apiMethod), arg: arg, cancellationToken: cancellationToken);

    public static async Task PutAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperForPostAsync<TIn>(uri: uri, arg: arg, cancellationToken: cancellationToken);

    private static async Task<HttpResponseMessage> HelperForPutAsync<TIn>(Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperAsync<TIn>(func: Client.PutAsync, uri: uri, arg: arg, cancellationToken: cancellationToken);

    public static async Task PutFileAsync(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
       => await PutFileAsync(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public static async Task PutFileAsync(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
        => await HelperFileAsync(func: Client.PutAsync, uri: uri, path: path, cancellationToken: cancellationToken);

    public static async Task<TOut?> PutFileAsync<TOut>(string apiMethod, string path, CancellationToken cancellationToken = default(CancellationToken))
       => await PutFileAsync<TOut>(uri: CreateUri(apiMethod), path: path, cancellationToken: cancellationToken);

    public static async Task<TOut?> PutFileAsync<TOut>(Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
    {
        HttpResponseMessage response = await HelperFileAsync(func: Client.PutAsync, uri: uri, path: path, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<TOut>(options: Options, cancellationToken: cancellationToken);
    }
    #endregion PUT

    #region DELETE

	public static async Task<TOut?> DeleteAsync<TOut>(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await HelperForDeleteAsync(uri: uri, cancellationToken: cancellationToken);
		return await responseMessage.Content.ReadFromJsonAsync<TOut>(cancellationToken: cancellationToken);
	}

	public static async Task<TOut?> DeleteAsync<TOut>(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync<TOut>(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public static async Task<TOut?> DeleteAsync<TOut>(string apiMethod, Dictionary<string, string> arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	public static async Task<TOut?> DeleteAsync<TIn, TOut>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync<TOut>(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	public static async Task DeleteAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		=> await HelperForDeleteAsync(uri: uri, cancellationToken: cancellationToken);

	public static async Task DeleteAsync(string apiMethod, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync(uri: CreateUri(apiMethod: apiMethod), cancellationToken: cancellationToken);

	public static async Task DeleteAsync(string apiMethod, Dictionary<string, string> arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	public static async Task DeleteAsync<TIn>(string apiMethod, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
		=> await DeleteAsync(uri: CreateUri(apiMethod: apiMethod, arg: arg), cancellationToken: cancellationToken);

	private static async Task<HttpResponseMessage> HelperForDeleteAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await Client.DeleteAsync(requestUri: uri, cancellationToken: cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: Options);
		return responseMessage;
	}
    #endregion DELETE

	public static void SetToken(string token)
		=> Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: token);

	public static void ResetToken()
		=> Client.DefaultRequestHeaders.Authorization = null;

	private static async Task<HttpResponseMessage> HelperFileAsync(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		using MultipartFormDataContent data = new MultipartFormDataContent();
		byte[] fileBytes = await File.ReadAllBytesAsync(path: path, cancellationToken: cancellationToken);
		ByteArrayContent content = new ByteArrayContent(content: fileBytes);
		data.Add(content: content, name: "file", fileName: Path.GetFileName(path: path));

		HttpResponseMessage responseMessage = await func(uri, data, cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: Options);
		return responseMessage;
	}

	private static async Task<TOut?> HelperAsync<TOut>(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await func(uri, new StringContent(content: String.Empty), cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: Options);
		return await responseMessage.Content.ReadFromJsonAsync<TOut>(options: Options, cancellationToken: cancellationToken);
	}

	private static async Task<HttpResponseMessage> HelperAsync<TIn>(Func<Uri, HttpContent, CancellationToken, Task<HttpResponseMessage>> func, Uri uri, TIn arg, CancellationToken cancellationToken = default(CancellationToken))
	{
		HttpResponseMessage responseMessage = await func(uri, JsonContent.Create(inputValue: arg, inputType: arg!.GetType()), cancellationToken);
		await ApiException.ThrowIfErrorAsync(message: responseMessage, options: Options);
		return responseMessage;
	}

	public static Uri CreateUri(string apiMethod)
		=> new Uri(uriString: ServerAddress + apiMethod);

	public static Uri CreateUri(string apiMethod, Dictionary<string, string> arg)
	{
		StringBuilder uri = new StringBuilder(value: ServerAddress + apiMethod + '?');
		foreach (KeyValuePair<string, string> pair in arg)
			uri.Append(value: $"{pair.Key}={pair.Value}&");

		uri.Remove(startIndex: uri.Length - 1, length: 1);
		return new Uri(uriString: uri.ToString());
	}

	public static Uri CreateUri<T>(string apiMethod, T arg)
	{
		StringBuilder uri = new StringBuilder(value: ServerAddress + apiMethod + '?');
		foreach (PropertyInfo pair in typeof(T).GetProperties())
			uri.Append(value: $"{pair.Name}={pair.GetValue(obj: arg)}&");

		uri.Remove(startIndex: uri.Length - 1, length: 1);
		return new Uri(uriString: uri.ToString());
	}
	#endregion
}