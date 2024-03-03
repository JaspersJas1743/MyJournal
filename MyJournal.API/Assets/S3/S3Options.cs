namespace MyJournal.API.Assets.S3;

public sealed class S3Options
{
	public string AccessKeyId { get; set; } = null!;
	public string SecretAccessKey { get; set; } = null!;
	public string Endpoint { get; set; } = null!;
	public string BucketName { get; set; } = null!;
}

public static class S3OptionsExtension
{
	public static S3Options GetS3Options(this IConfiguration configuration)
	{
		return configuration.GetSection(key: "AWS").Get<S3Options>()
			?? throw new ArgumentNullException(message: "Данные для AWS S3 отсутствуют или некорректны.", paramName: nameof(S3Options));
	}
}