using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		string connectionString = builder.Configuration.GetConnectionString("MyJournal")
			?? throw new ArgumentException(message: "Строка подключения отсутствует или некорректна", paramName: nameof(connectionString));

		builder.Services.AddDbContext<MyJournalContext>(
			optionsAction: options => options.UseSqlServer(connectionString: connectionString)
		);

		builder.Services.AddControllers().AddJsonOptions(
			configure: options =>
			{
				options.JsonSerializerOptions.WriteIndented = true;
				options.JsonSerializerOptions.PropertyNamingPolicy = null;
				options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			}
		);

		builder.Services.AddEndpointsApiExplorer();

		builder.Services.AddSwaggerGen();

		builder.Services.AddHealthChecks().AddSqlServer(
			connectionString: connectionString,
			tags: new string[] { "db", "database" }
		);

		builder.Services.AddHealthChecksUI().AddInMemoryStorage();

		var app = builder.Build();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

		app.MapHealthChecks(pattern: "/health", options: CreateHealthCheckOptions(predicate: _ => true));
		app.MapHealthChecks(pattern: "/health/db", options: CreateHealthCheckOptions(predicate: reg => reg.Tags.Contains(item: "db")));

		// добавить Redis в будущем
		// добавить Amazon AWS S3
		// добавить SignalR

		app.MapHealthChecksUI(u => u.UIPath = "/health-ui");

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