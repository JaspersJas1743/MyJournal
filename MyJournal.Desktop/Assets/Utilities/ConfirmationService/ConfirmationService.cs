using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Models;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.Views;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.ConfirmationService;

public sealed class ConfirmationService(MainWindowView mainWindow) : IConfirmationService
{
	public async Task Сonfirm(string text, ReactiveCommand<string, string>? command)
	{
		ConfirmationCodeWindowModel model = new ConfirmationCodeWindowModel()
		{
			Text = text,
			Command = command
		};
		IConfirmationService.Instance = new ConfirmationCodeWindow()
		{
			DataContext = new ConfirmationCodeWindowVM(model: model)
		};
		await IConfirmationService.Instance.ShowDialog<bool>(owner: mainWindow);
	}
}

public static class ConfirmationServiceExtensions
{
	public static IServiceCollection AddConfirmationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddSingleton<IConfirmationService, ConfirmationService>();

	public static IServiceCollection AddKeyedConfirmationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedSingleton<IConfirmationService, ConfirmationService>(serviceKey: key);
}