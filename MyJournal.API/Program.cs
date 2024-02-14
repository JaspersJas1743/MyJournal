using System.Diagnostics;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.S3;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API;

public class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

		string dbConnectionString = builder.Configuration.GetConnectionString(name: "MyJournalDB")
			?? throw new ArgumentException(message: "Строка подключения к MyJournalDB отсутствует или некорректна", paramName: nameof(dbConnectionString));

		builder.Services.AddDbContext<MyJournalContext>(
			optionsAction: options => options.UseSqlServer(connectionString: dbConnectionString)
		);

		string redisConnectionString = builder.Configuration.GetConnectionString(name: "MyJournalRedis")
			?? throw new ArgumentException(message: "Строка подключения к MyJournalRedis отсутствует или некорректна", paramName: nameof(dbConnectionString));

		builder.Services.AddStackExchangeRedisCache(
			setupAction: options => options.Configuration = redisConnectionString
		);

		S3Configuration? configuration = builder.Configuration.GetS3Configuration();
		AWSOptions awsOptions = new AWSOptions
		{
			DefaultClientConfig = { ServiceURL = configuration?.Endpoint },
			Credentials = new BasicAWSCredentials(accessKey: configuration?.AccessKeyId, secretKey: configuration?.SecretAccessKey)
		};

		builder.Services.AddAWSService<IAmazonS3>(options: awsOptions);
		builder.Services.AddScoped<IFileStorageService, FileStorageService>();

		builder.Services.AddControllers().AddJsonOptions(configure: options =>
		{
			options.JsonSerializerOptions.WriteIndented = true;
			options.JsonSerializerOptions.PropertyNamingPolicy = null;
			options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
		});

		builder.Services.AddEndpointsApiExplorer();

		builder.Services.AddSwaggerGen();

		builder.Services.AddHealthChecks()
			   .AddSqlServer(
				   connectionString: dbConnectionString,
				   name: "MyJournal database",
				   tags: new string[] { "db", "database" })
			   .AddRedis(
				   name: "MyJournal redis",
				   redisConnectionString: redisConnectionString,
				   tags: new string[] { "redis" })
			   .AddS3(
				   setup: options =>
				   {
					   options.Credentials = awsOptions.Credentials;
					   options.BucketName = configuration?.BucketName ?? throw new ArgumentNullException(paramName: nameof(configuration.BucketName));
					   options.S3Config = new AmazonS3Config()
					   {
						   ServiceURL = awsOptions.DefaultClientConfig.ServiceURL
					   };
				   },
				   name: "MyJournal AWS S3",
				   tags: new string[] { "aws s3", "s3", "aws" });

		string healthDbConnectionString = builder.Configuration.GetConnectionString(name: "MyJournalHealthDB")
			?? throw new ArgumentException(message: "Строка подключения к MyJournalHealthDB отсутствует или некорректна", paramName: nameof(healthDbConnectionString));
		builder.Services.AddHealthChecksUI().AddSqlServerStorage(connectionString: healthDbConnectionString);

		WebApplication app = builder.Build();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

		app.MapHealthChecks(pattern: "/health", options: CreateHealthCheckOptions(predicate: _ => true));
		app.MapHealthChecks(pattern: "/health/db", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "db")));
		app.MapHealthChecks(pattern: "/health/redis", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "redis")));
		app.MapHealthChecks(pattern: "/health/s3", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "s3")));
		app.MapHealthChecks(pattern: "/health/aws", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "aws")));

		// добавить SignalR

		app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

		app.UseAuthorization();

		app.MapControllers();

		app.Run();
	}

	private static HealthCheckOptions CreateHealthCheckOptions(Func<HealthCheckRegistration, bool> predicate)
		=> new HealthCheckOptions()
		{
			Predicate = predicate,
			ResponseWriter = HealthCheckResponseWriter.WriteResponse
		};
}