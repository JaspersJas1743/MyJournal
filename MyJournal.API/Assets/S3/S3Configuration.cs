namespace MyJournal.API.Assets.S3;

public class S3Configuration
{
	public string AccessKeyId { get; set; }
	public string SecretAccessKey { get; set; }
	public string Endpoint { get; set; }
	public string BucketName { get; set; }
}

public static class S3ConfigurationExtension
{
	public static S3Configuration? GetS3Configuration(this IConfiguration configuration)
		=> configuration.GetSection("AWS").Get<S3Configuration>();
}