namespace MyJournal.API.Assets.S3;

public interface IFileStorageService
{
	Task<string> UploadLogAsync(string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken));
	Task<string> UploadFileAsync(string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken));
	Task<Stream> GetFileAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
	Task DeleteFileAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
}