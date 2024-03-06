using Amazon.S3;
using Amazon.S3.Model;

namespace MyJournal.API.Assets.S3;

public sealed class FileStorageService(
	IAmazonS3 amazonS3Client
) : IFileStorageService
{
	private const string ColdBucketName = "myjournal_logs";
	private const string HotBucketName = "myjournal_assets";

	private async Task<string> UploadAsync(string bucket, string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		_ = await amazonS3Client.PutObjectAsync(request: new PutObjectRequest()
		{
			BucketName = bucket,
			Key = key,
			InputStream = fileStream,
			CannedACL = S3CannedACL.PublicRead
		}, cancellationToken: cancellationToken);
		return $"https://{bucket}.hb.ru-msk.vkcs.cloud/{key}";
	}

	public async Task<string> UploadLogAsync(string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
		=> await UploadAsync(bucket: ColdBucketName, key: key, fileStream: fileStream, cancellationToken: cancellationToken);

	public async Task<string> UploadFileAsync(string key, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
		=> await UploadAsync(bucket: HotBucketName, key: key, fileStream: fileStream, cancellationToken: cancellationToken);

	public async Task<Stream> GetFileAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
	{
		GetObjectResponse? result = await amazonS3Client.GetObjectAsync(request: new GetObjectRequest()
		{
			BucketName = HotBucketName,
			Key = key
		}, cancellationToken: cancellationToken);
		return result.ResponseStream;
	}

	public async Task DeleteFileAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
	{
		_ = await amazonS3Client.DeleteObjectAsync(request: new DeleteObjectRequest()
		{
			BucketName = HotBucketName,
			Key = key,
		}, cancellationToken: cancellationToken);
	}
}