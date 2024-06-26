using System.Reflection;
using System.Text.Json.Serialization;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.GoogleAuthenticator;
using MyJournal.API.Assets.Hubs;
using MyJournal.API.Assets.RegistrationCodeGenerator;
using MyJournal.API.Assets.S3;
using MyJournal.API.Assets.Security.Hash;
using MyJournal.API.Assets.Security.JWT;
using MyJournal.API.Assets.Utilities;
using MyJournal.API.Assets.Utilities.DisabledTokenFilter;
using MyJournal.API.Assets.Validation;

namespace MyJournal.API;

public class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

		string dbConnectionString = Environment.GetEnvironmentVariable(variable: "MyJournalDB")
			?? throw new ArgumentException(message: "Строка подключения к MyJournalDB отсутствует или некорректна", paramName: nameof(dbConnectionString));

		builder.Services.AddDbContext<MyJournalContext>(
			optionsAction: options => options.UseSqlServer(connectionString: dbConnectionString)
		);

		S3Options configuration = S3Options.GetFromEnvironmentVariables();
		AWSOptions awsOptions = new AWSOptions
		{
			DefaultClientConfig = { ServiceURL = configuration.Endpoint },
			Credentials = new BasicAWSCredentials(accessKey: configuration.AccessKeyId, secretKey: configuration.SecretAccessKey)
		};

		builder.Services.AddAWSService<IAmazonS3>(options: awsOptions);
		builder.Services.AddScoped<IFileStorageService, FileStorageService>();

		builder.Services.AddControllers(configure: options =>
			options.AddValidateModeFilter()
				   .AddDisabledTokenFilter()
				   .AddAutoValidation()
		).ConfigureApiBehaviorOptions(
			setupAction: options => options.InvalidModelStateResponseFactory = context => new ValidationFailedResult(modelState: context.ModelState)
		).AddJsonOptions(configure: options =>
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

			string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			options.IncludeXmlComments(filePath: Path.Combine(path1: AppContext.BaseDirectory, path2: xmlFilename));
		});

		builder.Services.AddHealthChecks()
			   .AddSqlServer(
				   connectionString: dbConnectionString,
				   name: "MyJournal database",
				   tags: new string[] { "db", "database" })
			   .AddS3(
				   setup: options =>
				   {
					   options.Credentials = awsOptions.Credentials;
					   options.BucketName = configuration.BucketName;
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

		string healthDbConnectionString = Environment.GetEnvironmentVariable(variable: "MyJournalHealthDB")
			?? throw new ArgumentException(message: "Строка подключения к MyJournalHealthDB отсутствует или некорректна", paramName: nameof(healthDbConnectionString));
		builder.Services.AddHealthChecksUI(setupSettings: settings =>
		{
			settings.AddHealthCheckEndpoint(name: "Health for databases", uri: "https://my-journal.ru/health/db");
			settings.AddHealthCheckEndpoint(name: "Health for db context", uri: "https://my-journal.ru/health/context");
			settings.AddHealthCheckEndpoint(name: "Health for AWS", uri: "https://my-journal.ru/health/aws");
			settings.AddHealthCheckEndpoint(name: "Health for S3 Storage", uri: "https://my-journal.ru/health/s3");
			settings.SetEvaluationTimeInSeconds(seconds: 10);
		}).AddSqlServerStorage(connectionString: healthDbConnectionString);

		JwtOptions jwtOptions = JwtOptions.GetFromEnvironmentVariables();
		builder.Services.AddSingleton<JwtOptions>(implementationInstance: jwtOptions);
		builder.Services.AddScoped<IJwtService, JwtService>();

		builder.Services.AddMemoryCache();
		builder.Services.Configure<IpRateLimitOptions>(options =>
		{
			options.EnableEndpointRateLimiting = true;
			options.StackBlockedRequests = false;
			options.HttpStatusCode = StatusCodes.Status429TooManyRequests;
			options.RealIpHeader = "X-Forwarded-For";
			options.ClientIdHeader = "X-ClientId";
			options.GeneralRules = new List<RateLimitRule>
			{
				new RateLimitRule
				{
					Endpoint = "*",
					PeriodTimespan = TimeSpan.FromSeconds(value: 1),
					Limit = 50,
				}
			};
		});

		builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
		builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
		builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
		builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
		builder.Services.AddInMemoryRateLimiting();

		builder.Services.AddAuthorization(configure: options =>
		{
			options.AddPolicy(name: nameof(UserRoles.Student), configurePolicy: policyBuilder =>
				policyBuilder.RequireClaim(claimType: MyJournalClaimTypes.Role, allowedValues: nameof(UserRoles.Student))
			);
			options.AddPolicy(name: nameof(UserRoles.Teacher), configurePolicy: policyBuilder =>
				policyBuilder.RequireClaim(claimType: MyJournalClaimTypes.Role, allowedValues: nameof(UserRoles.Teacher))
			);
			options.AddPolicy(name: nameof(UserRoles.Administrator), configurePolicy: policyBuilder =>
				policyBuilder.RequireClaim(claimType: MyJournalClaimTypes.Role, allowedValues: nameof(UserRoles.Administrator))
			);
			options.AddPolicy(name: nameof(UserRoles.Parent), configurePolicy: policyBuilder =>
				policyBuilder.RequireClaim(claimType: MyJournalClaimTypes.Role, allowedValues: nameof(UserRoles.Parent))
			);
			options.AddPolicy(name: nameof(UserRoles.Teacher) + nameof(UserRoles.Administrator), configurePolicy: policyBuilder =>
				policyBuilder.RequireClaim(claimType: MyJournalClaimTypes.Role, allowedValues: new string[] { nameof(UserRoles.Teacher), nameof(UserRoles.Administrator) })
			);
		});
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

		builder.Services.AddScoped<IRegistrationCodeGeneratorService, RegistrationCodeGeneratorService>();

		builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
		builder.Services.AddSignalR();

		builder.Services.AddCors(setupAction: options => options.AddPolicy(name: "CORSPolicy", configurePolicy: policyBuilder =>
		{
			policyBuilder.AllowAnyMethod();
			policyBuilder.AllowAnyHeader();
			policyBuilder.AllowCredentials();
			policyBuilder.SetIsOriginAllowed(_ => true);
		}));

		builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
		builder.Services.AddProblemDetails();

		builder.Services.Configure<FormOptions>(configureOptions: options =>
			options.MultipartBodyLengthLimit = 31457280 // 30Мбайт
		);

		builder.Services.AddScoped<IGoogleAuthenticatorService, GoogleAuthenticatorService>();

		WebApplication app = builder.Build();

		app.UseSwagger().UseSwaggerUI();

		app.MapHealthChecks(pattern: "/health", options: CreateHealthCheckOptions(predicate: _ => true));
		app.MapHealthChecks(pattern: "/health/db", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "db")));
		app.MapHealthChecks(pattern: "/health/s3", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "s3")));
		app.MapHealthChecks(pattern: "/health/aws", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "aws")));
		app.MapHealthChecks(pattern: "/health/context", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "context")));

		app.MapHealthChecksUI(setupOptions: options => options.UIPath = "/health-ui");

		app.UseIpRateLimiting();

		app.UseAuthentication();

		app.UseAuthorization();

		app.MapControllers();

		app.UseCors(policyName: "CORSPolicy");
		app.MapHub<UserHub>(pattern: "/hub/user");
		app.MapHub<TeacherHub>(pattern: "/hub/teacher");
		app.MapHub<StudentHub>(pattern: "/hub/student");
		app.MapHub<ParentHub>(pattern: "/hub/parent");
		app.MapHub<AdministratorHub>(pattern: "/hub/administrator");

		app.UseForwardedHeaders(options: new ForwardedHeadersOptions()
		{
			ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
		});

		app.UseExceptionHandler();

		app.Run();
	}

	private static HealthCheckOptions CreateHealthCheckOptions(Func<HealthCheckRegistration, bool> predicate)
		=> new HealthCheckOptions()
		{
			Predicate = predicate,
			ResponseWriter = HealthCheckResponseWriter.WriteResponse
		};
}