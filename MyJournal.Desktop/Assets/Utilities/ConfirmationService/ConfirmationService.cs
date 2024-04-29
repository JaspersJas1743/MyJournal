using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Models.ConfirmationCode;
using MyJournal.Desktop.ViewModels.ConfirmationCode;
using MyJournal.Desktop.Views;
using ReactiveUI;
using ConfirmationCodeWindow = MyJournal.Desktop.Views.ConfirmationCode.ConfirmationCodeWindow;

namespace MyJournal.Desktop.Assets.Utilities.ConfirmationService;

public sealed class ConfirmationService(MainWindowView mainWindow) : IConfirmationService
{
	public async Task Сonfirm(string text, ReactiveCommand<string, CommandExecuteResult>? command)
	{
		IConfirmationService.Instance = new ConfirmationCodeWindow()
		{
			DataContext = new ConfirmationCodeWindowVM(model: new ConfirmationCodeWindowModel(
				firstStepOfConfirmationVM: new FirstStepOfConfirmationVM(model: new FirstStepOfConfirmationModel()
				{
					Text = text,
					Command = command
				})
			))
		};
		await IConfirmationService.Instance.ShowDialog<bool>(owner: mainWindow);
		IConfirmationService.Instance = null;
	}
}

public static class ConfirmationServiceExtensions
{
	public static IServiceCollection AddConfirmationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddSingleton<IConfirmationService, ConfirmationService>();

	public static IServiceCollection AddKeyedConfirmationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedSingleton<IConfirmationService, ConfirmationService>(serviceKey: key);
}