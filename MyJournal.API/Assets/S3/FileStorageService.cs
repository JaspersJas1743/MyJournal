using Amazon.S3;
using Amazon.S3.Model;

namespace MyJournal.API.Assets.S3;

public class FileStorageService : IFileStorageService
{
	private readonly IAmazonS3 _amazonS3Client;
	private const string COLD_BUCKET_NAME = "myjournal_logs";
	private const string HOT_BUCKET_NAME = "myjournal_assets";

	public FileStorageService(IAmazonS3 amazonS3Client)
	{
		_amazonS3Client = amazonS3Client;
	}

	private async Task UploadAsync(string bucket, string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		_ = await _amazonS3Client.PutObjectAsync(request: new PutObjectRequest()
		{
			BucketName = bucket,
			Key = key,
			InputStream = fileStream
		}, cancellationToken: cancellationToken);
	}

	public async Task UploadLogAsync(string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
		=> await UploadAsync(bucket: COLD_BUCKET_NAME, key: key, fileStream: fileStream, cancellationToken: cancellationToken);

	public async Task UploadFileAsync(string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
		=> await UploadAsync(bucket: HOT_BUCKET_NAME, key: key, fileStream: fileStream, cancellationToken: cancellationToken);

	public async Task<Stream> GetFileAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
	{
		GetObjectResponse? result = await _amazonS3Client.GetObjectAsync(request: new GetObjectRequest()
		{
			BucketName = HOT_BUCKET_NAME,
			Key = key
		}, cancellationToken: cancellationToken);
		return result.ResponseStream;
	}

	public async Task DeleteFileAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
	{
		_ = await _amazonS3Client.DeleteObjectAsync(request: new DeleteObjectRequest()
		{
			BucketName = HOT_BUCKET_NAME,
			Key = key,
		}, cancellationToken: cancellationToken);
	}
}