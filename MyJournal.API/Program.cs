using Microsoft.EntityFrameworkCore;
using MyJournal.API.Assets.DatabaseModels;

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

		builder.Services.AddControllers();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		var app = builder.Build();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();
		app.UseAuthorization();
		app.MapControllers();
		app.Run();
	}
}