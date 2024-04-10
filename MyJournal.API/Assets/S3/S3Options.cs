namespace MyJournal.API.Assets.S3;

public sealed class S3Options
{
	public string AccessKeyId { get; init; } = null!;
	public string SecretAccessKey { get; init; } = null!;
	public string Endpoint { get; init; } = null!;
	public string BucketName { get; init; } = null!;

	public static S3Options GetFromEnvironmentVariables()
	{
		return new S3Options()
		{
			Endpoint = Environment.GetEnvironmentVariable(variable: "AWSEndpoint")
				?? throw new ArgumentException(message: "Параметр AWSEndpoint отсутствует или некорректен.", paramName: "AWSEndpoint"),
			BucketName = Environment.GetEnvironmentVariable(variable: "AWSBucketName")
				?? throw new ArgumentException(message: "Параметр AWSBucketName отсутствует или некорректен.", paramName: "AWSBucketName"),
			AccessKeyId = Environment.GetEnvironmentVariable(variable: "AWSAccessKeyId")
				?? throw new ArgumentException(message: "Параметр AWSAccessKeyId отсутствует или некорректен.", paramName: "AWSAccessKeyId"),
			SecretAccessKey = Environment.GetEnvironmentVariable(variable: "AWSSecretAccessKey")
				?? throw new ArgumentException(message: "Параметр AWSSecretAccessKey отсутствует или некорректен.", paramName: "AWSSecretAccessKey")
		};
	}
}