using System.Reflection;
using System.Text.Json.Serialization;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.S3;
using MyJournal.API.Assets.Security.Hash;
using MyJournal.API.Assets.Security.JWT;
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

		S3Options? configuration = builder.Configuration.GetS3Options();
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

		builder.Services.AddSwaggerGen(setupAction: options =>
		{
			options.SwaggerDoc(name: "v1", info: new OpenApiInfo()
			{
				Description = "Документация к API приложения MyJournal",
				Version = "v1",
				Title = "MyJournal API"
			});
			options.AddSecurityDefinition(name: "Bearer", securityScheme: new OpenApiSecurityScheme()
			{
				In = ParameterLocation.Header,
				Description = "Введите jwt-токен в формате: Bearer <токен>",
				Name = "Authorization",
				Type = SecuritySchemeType.ApiKey,
				Scheme = JwtBearerDefaults.AuthenticationScheme
			});
			options.AddSecurityRequirement(securityRequirement: new OpenApiSecurityRequirement {
			{
				new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = "Bearer"
					}
				},
				new string[] { }
			}});

			var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			options.IncludeXmlComments(filePath: Path.Combine(path1: AppContext.BaseDirectory, path2: xmlFilename));
		});

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
				   tags: new string[] { "aws s3", "s3", "aws" })
			   .AddDbContextCheck<MyJournalContext>(
				   name: "MyJournal DB Context",
				   tags: new string[] { "context", "db-context", "database" });

		string healthDbConnectionString = builder.Configuration.GetConnectionString(name: "MyJournalHealthDB")
			?? throw new ArgumentException(message: "Строка подключения к MyJournalHealthDB отсутствует или некорректна", paramName: nameof(healthDbConnectionString));
		builder.Services.AddHealthChecksUI().AddSqlServerStorage(connectionString: healthDbConnectionString);

		JwtOptions jwtOptions = builder.Configuration.GetJwtOptions();
		builder.Services.AddSingleton<JwtOptions>(implementationInstance: jwtOptions);
		builder.Services.AddScoped<IJwtService, JwtService>();

		builder.Services.AddMemoryCache();
		builder.Services.Configure<IpRateLimitOptions>(options =>
		{
			options.EnableEndpointRateLimiting = true;
			options.StackBlockedRequests = false;
			options.HttpStatusCode = StatusCodes.Status429TooManyRequests;
			options.RealIpHeader = "X-Real-IP";
			options.ClientIdHeader = "X-ClientId";
			options.GeneralRules = new List<RateLimitRule>
			{
				new RateLimitRule
				{
					Endpoint = "*",
					PeriodTimespan = TimeSpan.FromSeconds(value: 1),
					Limit = 10,
				}
			};
		});

		builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
		builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
		builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
		builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
		builder.Services.AddInMemoryRateLimiting();

		builder.Services.AddAuthorization();

		builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
			   .AddJwtBearer(configureOptions: options =>
			   {
				   options.TokenValidationParameters = new TokenValidationParameters()
				   {
					   ValidIssuer = jwtOptions.Issuer,
					   ValidateIssuer = true,
					   ValidAudience = jwtOptions.Audience,
					   ValidateAudience = true,
					   IssuerSigningKey = jwtOptions.SymmetricKey,
					   ValidateIssuerSigningKey = true,
					   ValidateLifetime = false
				   };
			   });

		builder.Services.AddScoped<IHashService, BCryptHashService>();

		WebApplication app = builder.Build();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.MapHealthChecks(pattern: "/health", options: CreateHealthCheckOptions(predicate: _ => true));
		app.MapHealthChecks(pattern: "/health/db", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "db")));
		app.MapHealthChecks(pattern: "/health/redis", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "redis")));
		app.MapHealthChecks(pattern: "/health/s3", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "s3")));
		app.MapHealthChecks(pattern: "/health/aws", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "aws")));
		app.MapHealthChecks(pattern: "/health/context", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "context")));

		// добавить SignalR

		app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

		app.UseIpRateLimiting();

		app.UseAuthentication();

		app.UseHttpsRedirection();

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