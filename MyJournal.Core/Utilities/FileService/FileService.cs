using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Utilities.FileService;

public sealed class FileService : IFileService
{
	public static readonly IFileService Empty = new FileService();

	private FileService() { }

	public FileService(ApiClient client)
		=> ApiClient = client;

	public ApiClient ApiClient { get; set; }

	private sealed record FileLink(string Link);

	public async Task Download(
		string link,
		string pathToSave,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (link is null)
			throw new ArgumentNullException(message: "Ссылка на файл не может быть null.", paramName: nameof(link));

		byte[] file = await ApiClient.GetBytesAsync<FileLink>(
			apiMethod: FileControllerMethods.DownloadFile,
			argQuery: new FileLink(Link: link),
			cancellationToken: cancellationToken
		);
		await File.WriteAllBytesAsync(
			path: Path.Combine(path1: pathToSave, path2: ApiClient.FileName),
			bytes: file,
			cancellationToken: cancellationToken
		);
	}

	public async Task<string?> Upload(
		string folderToSave,
        string pathToFile,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		FileLink? response = await ApiClient.PutFileAsync<FileLink>(
			apiMethod: FileControllerMethods.UploadFile(bucket: folderToSave),
			path: pathToFile,
			cancellationToken: cancellationToken
		);
		return response?.Link;
	}

	public async Task Delete(
        string link,
        CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (link is null)
			throw new ArgumentNullException(message: "Ссылка на файл не может быть null.", paramName: nameof(link));

		await ApiClient.DeleteAsync(
			uri: ApiClient.CreateUri(
				apiMethod: FileControllerMethods.DeleteFile,
				arg: new FileLink(Link: link)
			), cancellationToken: cancellationToken
		);
	}
}

public static class FileServiceExtensions
{
	public static IServiceCollection AddFileService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IFileService, FileService>();

	public static IServiceCollection AddKeyedFileService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<IFileService, FileService>(serviceKey: key);
}
