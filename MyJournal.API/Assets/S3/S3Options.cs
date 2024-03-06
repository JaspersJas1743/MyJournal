namespace MyJournal.API.Assets.S3;

public sealed class S3Options
{
	public string AccessKeyId { get; init; } = null!;
	public string SecretAccessKey { get; init; } = null!;
	public string Endpoint { get; init; } = null!;
	public string BucketName { get; init; } = null!;
}

public static class S3OptionsExtension
{
	public static S3Options GetS3Options(this IConfiguration configuration)
	{
		return configuration.GetSection(key: "AWS").Get<S3Options>()
			?? throw new ArgumentNullException(message: "Данные для AWS S3 отсутствуют или некорректны.", paramName: nameof(S3Options));
	}
}